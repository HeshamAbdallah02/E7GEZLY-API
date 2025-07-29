using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Tests.Categories;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace E7GEZLY_API.Tests.Integration.Services
{
    [TestClass]
    [TestCategory(TestCategories.Integration)]
    [TestCategory(TestCategories.Authentication)]
    public class TokenServiceIntegrationTests
    {
        private ServiceProvider? _serviceProvider;
        private ITokenService? _tokenService;
        private UserManager<ApplicationUser>? _userManager;
        private AppDbContext? _context;

        [TestInitialize]
        public async Task Setup()
        {
            var services = new ServiceCollection();

            // Add Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // Add configuration
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", "ThisIsATestSecretKeyForTestingOnly123456!"},
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "TestAudience"},
                    {"Jwt:ExpiryInMinutes", "60"}
                })
                .Build();

            services.AddSingleton<IConfiguration>(config);

            // Add DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            // Add Identity
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // Add HttpContextAccessor (required by TokenService)
            services.AddHttpContextAccessor();

            // Add TokenService
            services.AddScoped<ITokenService, TokenService>();

            _serviceProvider = services.BuildServiceProvider();

            // Get services
            _tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            _context = _serviceProvider.GetRequiredService<AppDbContext>();

            // Seed roles
            await DbInitializer.SeedRolesAsync(_serviceProvider);
        }

        [TestMethod]
        public async Task GenerateTokens_ForNewCustomer_ReturnsValidTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                PhoneNumber = "+201234567890",
                IsPhoneNumberVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userManager!.CreateAsync(user, "Test123!");
            await _userManager.AddToRoleAsync(user, DbInitializer.AppRoles.Customer);

            // Act
            var result = await _tokenService!.GenerateTokensAsync(user);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
            Assert.IsFalse(string.IsNullOrEmpty(result.RefreshToken));
            Assert.AreEqual("Customer", result.UserType);
            Assert.IsTrue(result.UserInfo.ContainsKey("email"));
            Assert.AreEqual("test@example.com", result.UserInfo["email"]);
        }

        [TestMethod]
        public async Task GenerateTokens_ForVenueAdmin_ReturnsValidTokensWithVenueId()
        {
            // Arrange
            var venueId = Guid.NewGuid();

            // Create Governorate first
            var governorate = new Governorate
            {
                Id = 1,
                NameAr = "القاهرة",
                NameEn = "Cairo"
            };
            _context!.Governorates.Add(governorate);

            // Create District
            var district = new District
            {
                Id = 1,
                NameAr = "مدينة نصر",
                NameEn = "Nasr City",
                GovernorateId = governorate.Id,
                Governorate = governorate
            };
            _context.Districts.Add(district);

            // Save governorate and district first
            await _context.SaveChangesAsync();

            // Create the venue
            var venue = new Venue
            {
                Id = venueId,
                Name = "Test Venue",
                VenueType = VenueType.PlayStationVenue,
                Latitude = 30.0444,
                Longitude = 31.2357,
                DistrictId = district.Id,
                District = district,
                StreetAddress = "123 Test Street",
                Landmark = "Near Test Mall",
                CreatedAt = DateTime.UtcNow
            };

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();

            // Now create the user
            var user = new ApplicationUser
            {
                UserName = "venue@example.com",
                Email = "venue@example.com",
                PhoneNumber = "+201234567890",
                IsPhoneNumberVerified = true,
                VenueId = venueId,
                CreatedAt = DateTime.UtcNow
            };

            await _userManager!.CreateAsync(user, "Test123!");
            await _userManager.AddToRoleAsync(user, DbInitializer.AppRoles.VenueAdmin);

            // Act
            var result = await _tokenService!.GenerateTokensAsync(user);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Venue", result.UserType);
            Assert.IsTrue(result.UserInfo.ContainsKey("venueId"));
            Assert.AreEqual(venueId.ToString(), result.UserInfo["venueId"]);
            Assert.IsTrue(result.UserInfo.ContainsKey("venueName"));
            Assert.AreEqual("Test Venue", result.UserInfo["venueName"]);
            Assert.IsTrue(result.UserInfo.ContainsKey("venueType"));
            Assert.AreEqual(VenueType.PlayStationVenue.ToString(), result.UserInfo["venueType"]);
        }

        [TestMethod]
        public void GenerateRefreshToken_CreatesUniqueTokens()
        {
            // Act
            var token1 = _tokenService!.GenerateRefreshToken();
            var token2 = _tokenService!.GenerateRefreshToken();

            // Assert
            Assert.IsNotNull(token1);
            Assert.IsNotNull(token2);
            Assert.AreNotEqual(token1, token2, "Each refresh token should be unique");
            Assert.IsTrue(token1.Length >= 32, "Refresh token should be sufficiently long");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}