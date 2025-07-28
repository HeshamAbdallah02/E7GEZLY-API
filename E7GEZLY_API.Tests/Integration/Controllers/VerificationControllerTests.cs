using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Tests.Categories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        [TestInitialize]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Set the content root explicitly
                    var projectDir = Directory.GetCurrentDirectory();
                    var configPath = Path.Combine(projectDir, "appsettings.json");

                    builder.UseContentRoot(projectDir);

                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile(configPath, optional: true);
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

                        // Add in-memory database for testing
                        services.AddDbContext<AppDbContext>(options =>
                        {
                            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                        });

                        // Build service provider
                        var sp = services.BuildServiceProvider();

                        // Create scope to seed database
                        using (var scope = sp.CreateScope())
                        {
                            var scopedServices = scope.ServiceProvider;
                            var db = scopedServices.GetRequiredService<AppDbContext>();

                            db.Database.EnsureCreated();

                            // Seed test data
                            DbInitializer.SeedRolesAsync(scopedServices).GetAwaiter().GetResult();
                        }
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
            // Arrange - Create a user first
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

            // Use the record constructor
            var sendDto = new SendVerificationCodeDto(
                UserId: userId,
                Method: VerificationMethod.Email
            );

            var json = JsonSerializer.Serialize(sendDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/auth/verify/send", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(responseContent.Contains("Success"));
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
                await userManager.AddToRoleAsync(user, DbInitializer.AppRoles.Customer);
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

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(responseContent.Contains("Tokens"));
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

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task SendVerificationCode_NonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var sendDto = new SendVerificationCodeDto(
                UserId: "non-existent-user-id",
                Method: VerificationMethod.Email
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