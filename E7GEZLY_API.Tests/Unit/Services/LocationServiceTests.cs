using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using E7GEZLY_API.Services.Location;
using E7GEZLY_API.Tests.Categories;
using E7GEZLY_API.Tests.TestHelpers;
using System.Threading.Tasks;
using System.Linq;
using Moq;

namespace E7GEZLY_API.Tests.Unit.Services
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    public class LocationServiceTests
    {
        private ILocationService? _locationService;
        private Mock<ILogger<LocationService>>? _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            var context = TestDataFactory.CreateTestDbContext();
            _mockLogger = new Mock<ILogger<LocationService>>();
            _locationService = new LocationService(context, _mockLogger.Object);
        }

        [TestMethod]
        public async Task GetGovernorates_ReturnsAllGovernorates()
        {
            // Arrange
            Assert.IsNotNull(_locationService);

            // Act
            var governorates = await _locationService.GetGovernoratesAsync();

            // Assert
            Assert.IsNotNull(governorates);
            Assert.AreEqual(2, governorates.Count());
            Assert.IsTrue(governorates.Any(g => g.NameEn == "Cairo"));
            Assert.IsTrue(governorates.Any(g => g.NameEn == "Giza"));
        }

        [TestMethod]
        public async Task GetDistricts_WithGovernorateId_ReturnsFilteredDistricts()
        {
            // Arrange
            Assert.IsNotNull(_locationService);

            // Act
            var districts = await _locationService.GetDistrictsAsync(1); // Cairo

            // Assert
            Assert.IsNotNull(districts);
            Assert.AreEqual(2, districts.Count());
            Assert.IsTrue(districts.All(d => d.GovernorateId == 1));
        }

        [TestMethod]
        public async Task GetDistricts_WithoutGovernorateId_ReturnsAllDistricts()
        {
            // Arrange
            Assert.IsNotNull(_locationService);

            // Act
            var districts = await _locationService.GetDistrictsAsync(null);

            // Assert
            Assert.IsNotNull(districts);
            Assert.AreEqual(2, districts.Count());
        }
    }
}