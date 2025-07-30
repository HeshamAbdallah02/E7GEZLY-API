using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Caching;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Tests.Categories;
using E7GEZLY_API.Tests.Mocks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace E7GEZLY_API.Tests.Integration.Controllers
{
    [TestClass]
    [TestCategory(TestCategories.Integration)]
    [TestCategory(TestCategories.Authentication)]
    [DoNotParallelize]
    public class VerificationControllerTests : IDisposable
    {
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _client;
        private JsonSerializerOptions? _jsonOptions;
        private string _databaseName = null!;

        [TestInitialize]
        public void Setup()
        {
            // Use a unique but consistent database name for this test run
            _databaseName = $"TestDb_{Guid.NewGuid()}";

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");

                    // Add test configuration
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile("testsettings.json", optional: false, reloadOnChange: true);
                    });

                    builder.ConfigureServices(services =>
                    {
                        // Remove the existing DbContext registration
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add in-memory database for testing with a specific name
                        services.AddDbContext<AppDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(_databaseName);
                            options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                        });

                        // Replace email service with mock
                        services.RemoveAll<IEmailService>();
                        var mockEmailService = new Mock<IEmailService>();
                        mockEmailService.Setup(x => x.SendVerificationEmailAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                            .ReturnsAsync(true);
                        mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                            .ReturnsAsync(true);
                        mockEmailService.Setup(x => x.SendWelcomeEmailAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                            .ReturnsAsync(true);

                        services.AddSingleton(mockEmailService.Object);

                        // Replace Redis cache with mock for tests
                        services.RemoveAll<ICacheService>();
                        services.RemoveAll<IConnectionMultiplexer>();
                        services.AddSingleton<ICacheService>(new MockCacheService());
                    });
                });

            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [TestMethod]
        public async Task SendVerificationCode_ValidUser_ReturnsSuccess()
        {
            // Arrange - Create a user first using the factory's services
            string userId;
            using (var scope = _factory!.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser
                {
                    UserName = "verifytest@example.com",
                    Email = "verifytest@example.com",
                    PhoneNumber = "+201234567890"
                };
                await userManager.CreateAsync(user, "Test123!");
                userId = user.Id;
            }

            // Use the record constructor - add Purpose parameter
            var sendDto = new SendVerificationCodeDto(
                UserId: userId,
                Method: VerificationMethod.Email,
                Purpose: VerificationPurpose.AccountVerification
            );

            var json = JsonSerializer.Serialize(sendDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/auth/verify/send", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Debug output if test fails
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");
            }

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(responseContent.Contains("success") || responseContent.Contains("Success"));
        }

        [TestMethod]
        public async Task VerifyAccount_ValidCode_ReturnsTokens()
        {
            // Arrange
            string userId;
            string verificationCode = "123456";

            using (var scope = _factory!.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser
                {
                    UserName = "verifyaccount@example.com",
                    Email = "verifyaccount@example.com",
                    PhoneNumber = "+201234567890",
                    EmailVerificationCode = verificationCode,
                    EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10)
                };
                await userManager.CreateAsync(user, "Test123!");

                // Use the correct role name (case sensitive)
                await userManager.AddToRoleAsync(user, "Customer");
                userId = user.Id;
            }

            // Use the record constructor
            var verifyDto = new VerifyAccountDto(
                UserId: userId,
                Method: VerificationMethod.Email,
                VerificationCode: verificationCode
            );

            var json = JsonSerializer.Serialize(verifyDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/auth/verify", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Debug output if test fails
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");
            }

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(responseContent.Contains("tokens") || responseContent.Contains("Tokens"));
        }

        [TestMethod]
        public async Task VerifyAccount_InvalidCode_ReturnsBadRequest()
        {
            // Arrange
            string userId;

            using (var scope = _factory!.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser
                {
                    UserName = "invalidcode@example.com",
                    Email = "invalidcode@example.com",
                    PhoneNumber = "+201234567890",
                    EmailVerificationCode = "123456",
                    EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10)
                };
                await userManager.CreateAsync(user, "Test123!");
                userId = user.Id;
            }

            // Use the record constructor
            var verifyDto = new VerifyAccountDto(
                UserId: userId,
                Method: VerificationMethod.Email,
                VerificationCode: "999999" // Wrong code
            );

            var json = JsonSerializer.Serialize(verifyDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/auth/verify", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Debug output if test fails
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");
            }

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task SendVerificationCode_NonExistentUser_ReturnsNotFound()
        {
            // Arrange - add Purpose parameter
            var sendDto = new SendVerificationCodeDto(
                UserId: "non-existent-user-id",
                Method: VerificationMethod.Email,
                Purpose: VerificationPurpose.AccountVerification
            );

            var json = JsonSerializer.Serialize(sendDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/auth/verify/send", content);

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task VerifyAccount_ExpiredCode_ReturnsBadRequest()
        {
            // Arrange
            string userId;
            string verificationCode = "123456";

            using (var scope = _factory!.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser
                {
                    UserName = "expiredcode@example.com",
                    Email = "expiredcode@example.com",
                    PhoneNumber = "+201234567890",
                    EmailVerificationCode = verificationCode,
                    EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(-10) // Expired
                };
                await userManager.CreateAsync(user, "Test123!");
                userId = user.Id;
            }

            var verifyDto = new VerifyAccountDto(
                UserId: userId,
                Method: VerificationMethod.Email,
                VerificationCode: verificationCode
            );

            var json = JsonSerializer.Serialize(verifyDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/auth/verify", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Debug output if test fails
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");
            }

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}