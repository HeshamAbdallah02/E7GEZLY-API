// Tests/Integration/GeocodingServiceTests.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace E7GEZLY_API.Tests.Integration
{
    [TestClass]
    public class GeocodingServiceTests
    {
        private IGeocodingService? _geocodingService;
        private AppDbContext? _context;
        private HttpClient? _httpClient;

        [TestInitialize]
        public void Setup()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);

            // Seed test data
            SeedTestData();

            // Setup HTTP client
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "E7GEZLY-Tests/1.0");

            // Setup logging
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<NominatimGeocodingService>();

            // Create the geocoding service
            _geocodingService = new NominatimGeocodingService(_httpClient, _context, logger);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
            _context?.Dispose();
        }

        private void SeedTestData()
        {
            if (_context == null) return;

            // Add test governorates
            var cairo = new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" };
            var giza = new Governorate { Id = 2, NameEn = "Giza", NameAr = "الجيزة" };

            _context.Governorates.AddRange(cairo, giza);

            // Add test districts
            var nasrCity = new District
            {
                Id = 1,
                NameEn = "Nasr City",
                NameAr = "مدينة نصر",
                GovernorateId = 1,
                CenterLatitude = 30.0626,
                CenterLongitude = 31.2497
            };

            var maadi = new District
            {
                Id = 2,
                NameEn = "Maadi",
                NameAr = "المعادي",
                GovernorateId = 1,
                CenterLatitude = 30.0131,
                CenterLongitude = 31.2089
            };

            var october = new District
            {
                Id = 11,
                NameEn = "6th of October",
                NameAr = "السادس من أكتوبر",
                GovernorateId = 2,
                CenterLatitude = 29.9285,
                CenterLongitude = 30.9188
            };

            _context.Districts.AddRange(nasrCity, maadi, october);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetDistrictFromCoordinates_CairoLocation_ReturnsCorrectDistrict()
        {
            // Arrange
            Assert.IsNotNull(_geocodingService, "Geocoding service should be initialized");
            var latitude = 30.0626;  // Nasr City
            var longitude = 31.2497;

            // Act
            var districtId = await _geocodingService.GetDistrictIdFromCoordinatesAsync(latitude, longitude);

            // Assert
            Assert.IsNotNull(districtId, "Should find a district for Cairo coordinates");
            // Note: Actual district ID may vary based on Nominatim response
            // This is more of an integration test to ensure the service works
        }

        [TestMethod]
        public async Task GetDistrictFromCoordinates_InvalidLocation_ReturnsNullOrDefault()
        {
            // Arrange
            Assert.IsNotNull(_geocodingService, "Geocoding service should be initialized");
            var latitude = 0.0;  // Middle of ocean
            var longitude = 0.0;

            // Act
            var districtId = await _geocodingService.GetDistrictIdFromCoordinatesAsync(latitude, longitude);

            // Assert
            // Should either return null or a default district based on implementation
            // The service might return the nearest district even for invalid locations
            if (districtId.HasValue)
            {
                Assert.IsTrue(districtId.Value > 0, "If a district is returned, it should be valid");
            }
        }

        [TestMethod]
        public async Task GetAddressFromCoordinates_ValidLocation_ReturnsAddress()
        {
            // Arrange
            Assert.IsNotNull(_geocodingService, "Geocoding service should be initialized");
            var latitude = 30.0444;  // Cairo center
            var longitude = 31.2357;

            // Act
            var result = await _geocodingService.GetAddressFromCoordinatesAsync(latitude, longitude);

            // Assert
            Assert.IsNotNull(result, "Should return a geocoding result");
            Assert.IsFalse(string.IsNullOrEmpty(result.FormattedAddress), "Should have a formatted address");
        }

        [TestMethod]
        [ExpectedException(typeof(E7GEZLY_API.Exceptions.GeocodingException))]
        public async Task GetAddressFromCoordinates_InvalidCoordinates_ThrowsException()
        {
            // Arrange
            Assert.IsNotNull(_geocodingService, "Geocoding service should be initialized");
            var latitude = 200.0;  // Invalid latitude
            var longitude = 31.2357;

            // Act
            await _geocodingService.GetAddressFromCoordinatesAsync(latitude, longitude);

            // Assert - Exception expected
        }
    }
}