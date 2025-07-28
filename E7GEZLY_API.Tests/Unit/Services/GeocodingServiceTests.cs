using Microsoft.VisualStudio.TestTools.UnitTesting;
using E7GEZLY_API.Services.Location;
using E7GEZLY_API.Tests.Categories;
using E7GEZLY_API.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E7GEZLY_API.Tests.Unit.Services
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    [TestCategory(TestCategories.Geocoding)]
    public class GeocodingServiceTests
    {
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;
        private NominatimGeocodingService? _geocodingService;

        [TestInitialize]
        public void Setup()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);

            var context = TestDataFactory.CreateTestDbContext();
            var logger = new NullLogger<NominatimGeocodingService>();

            _geocodingService = new NominatimGeocodingService(_httpClient, context, logger);
        }

        [TestMethod]
        public async Task GetAddressFromCoordinates_ValidResponse_ReturnsDistrict()
        {
            // Arrange
            var mockResponse = @"{
                ""display_name"": ""Nasr City, Cairo, Egypt"",
                ""address"": {
                    ""suburb"": ""Nasr City"",
                    ""city"": ""Cairo"",
                    ""state"": ""Cairo Governorate""
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
            var result = await _geocodingService!.GetAddressFromCoordinatesAsync(30.0626, 31.2497);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Nasr City", result.Suburb);
            Assert.AreEqual("Cairo", result.City);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }
    }
}