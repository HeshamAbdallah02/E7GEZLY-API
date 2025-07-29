using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.VenueManagement;
using E7GEZLY_API.Tests.Categories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace E7GEZLY_API.Tests.Unit.Services
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    public class VenueProfileServiceTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<ILogger<VenueProfileService>> _loggerMock = null!;
        private AppDbContext _context = null!;
        private VenueProfileService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new AppDbContext(options);

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _loggerMock = new Mock<ILogger<VenueProfileService>>();

            _service = new VenueProfileService(
                _context,
                _userManagerMock.Object,
                _loggerMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        [TestMethod]
        public async Task CompleteCourtProfileAsync_ShouldCompleteProfile_WhenValidData()
        {
            // Arrange
            var userId = "test-user-id";
            var user = new ApplicationUser { Id = userId };
            var venue = new Venue
            {
                Id = Guid.NewGuid(),
                Name = "Test Court",
                VenueType = VenueType.FootballCourt,
                IsProfileComplete = false
            };
            user.VenueId = venue.Id;
            user.Venue = venue;

            var dto = new CompleteCourtProfileDto
            {
                Latitude = 30.0444,
                Longitude = 31.2357,
                DistrictId = 1,
                StreetAddress = "123 Test St",
                MorningStartTime = new TimeSpan(6, 0, 0),
                MorningEndTime = new TimeSpan(12, 0, 0),
                EveningStartTime = new TimeSpan(14, 0, 0),
                EveningEndTime = new TimeSpan(22, 0, 0),
                MorningHourPrice = 100,
                EveningHourPrice = 150,
                DepositPercentage = 25,
                WorkingHours = new List<WorkingHoursDto>
                {
                    new() { DayOfWeek = DayOfWeek.Monday, OpenTime = new TimeSpan(6, 0, 0), CloseTime = new TimeSpan(22, 0, 0) }
                }
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(_context.Users.AsQueryable());

            await _context.Users.AddAsync(user);
            await _context.Venues.AddAsync(venue);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CompleteCourtProfileAsync(userId, dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsProfileComplete);
            Assert.AreEqual("Venue profile completed successfully", result.Message);
        }

        [TestMethod]
        public async Task CompletePlayStationProfileAsync_ShouldRequirePS4OrPS5()
        {
            // Arrange
            var userId = "test-user-id";
            var dto = new CompletePlayStationProfileDto
            {
                Latitude = 30.0444,
                Longitude = 31.2357,
                DistrictId = 1,
                NumberOfRooms = 10,
                HasPS4 = false,
                HasPS5 = false, // Both false should fail
                WorkingHours = new List<WorkingHoursDto>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CompletePlayStationProfileAsync(userId, dto));
        }

        [TestMethod]
        public async Task ValidateVenueTypeAsync_ShouldReturnCorrectType()
        {
            // Arrange
            var userId = "test-user-id";
            var user = new ApplicationUser { Id = userId };
            var venue = new Venue
            {
                Id = Guid.NewGuid(),
                Name = "Test PlayStation Venue",  // Add Name property
                VenueType = VenueType.PlayStationVenue
            };
            user.Venue = venue;
            user.VenueId = venue.Id;

            _userManagerMock.Setup(x => x.Users)
                .Returns(_context.Users.AsQueryable());

            await _context.Users.AddAsync(user);
            await _context.Venues.AddAsync(venue);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ValidateVenueTypeAsync(userId, VenueType.PlayStationVenue);

            // Assert
            Assert.IsTrue(result);
        }
    }
}