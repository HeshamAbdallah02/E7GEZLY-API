// Tests/Unit/GeocodingServiceUnitTests.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E7GEZLY_API.Tests.Unit
{
    [TestClass]
    public class GeocodingServiceUnitTests
    {
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;
        private AppDbContext? _context;
        private NominatimGeocodingService? _geocodingService;

        [TestInitialize]
        public void Setup()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _context = new AppDbContext(options);
            SeedTestData();

            // Setup mock HTTP handler
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockHttpHandler.Object);

            // Setup logger
            var logger = new Mock<ILogger<NominatimGeocodingService>>().Object;

            // Create service
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

            var cairo = new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" };
            _context.Governorates.Add(cairo);

            var nasrCity = new District
            {
                Id = 1,
                NameEn = "Nasr City",
                NameAr = "مدينة نصر",
                GovernorateId = 1,
                CenterLatitude = 30.0626,
                CenterLongitude = 31.2497
            };
            _context.Districts.Add(nasrCity);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetAddressFromCoordinates_SuccessfulResponse_ReturnsCorrectDistrict()
        {
            // Arrange
            var latitude = 30.0626;
            var longitude = 31.2497;

            var mockResponse = @"{
                ""display_name"": ""Nasr City, Cairo, Egypt"",
                ""address"": {
                    ""suburb"": ""Nasr City"",
                    ""city"": ""Cairo"",
                    ""state"": ""Cairo Governorate"",
                    ""road"": ""Test Street""
                }
            }";

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _geocodingService!.GetAddressFromCoordinatesAsync(latitude, longitude);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DistrictId); // Should match Nasr City
            Assert.AreEqual("Nasr City", result.DistrictName);
            Assert.AreEqual("Cairo", result.GovernorateName);
        }

        [TestMethod]
        public async Task GetAddressFromCoordinates_ApiReturnsError_ReturnsNull()
        {
            // Arrange
            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable
                });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<E7GEZLY_API.Exceptions.GeocodingException>(
                async () => await _geocodingService!.GetAddressFromCoordinatesAsync(30.0, 31.0)
            );
        }

        [TestMethod]
        public async Task GetAddressFromCoordinates_NetworkTimeout_ThrowsException()
        {
            // Arrange
            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<E7GEZLY_API.Exceptions.GeocodingException>(
                async () => await _geocodingService!.GetAddressFromCoordinatesAsync(30.0, 31.0)
            );
        }

        [TestMethod]
        public async Task GetDistrictIdFromCoordinates_NoMatchFound_UsesFallbackToNearest()
        {
            // Arrange
            var mockResponse = @"{
                ""display_name"": ""Unknown Place, Egypt"",
                ""address"": {
                    ""country"": ""Egypt""
                }
            }";

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
                });
            // Act
            var districtId = await _geocodingService!.GetDistrictIdFromCoordinatesAsync(30.06, 31.24);

            // Assert
            Assert.IsNotNull(districtId);
            Assert.AreEqual(1, districtId.Value); // Should fall back to nearest district (Nasr City)
        }
    }
}