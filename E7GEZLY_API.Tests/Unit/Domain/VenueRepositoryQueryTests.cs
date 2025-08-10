using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.ValueObjects;
using E7GEZLY_API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E7GEZLY_API.Tests.Unit.Domain;

/// <summary>
/// Unit tests for VenueRepository query methods implemented in Task 2B
/// Tests venue search, filtering, and location-based query functionality
/// </summary>
public class VenueRepositoryQueryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly VenueRepository _repository;

    public VenueRepositoryQueryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new VenueRepository(_context);

        // Seed test data representing Egyptian venues
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test districts (Egyptian locations)
        var cairoDistrict = new E7GEZLY_API.Models.Location
        {
            SystemId = 1,
            NameEn = "Nasr City",
            NameAr = "مدينة نصر",
            Type = "District",
            GovernorateId = 1
        };

        var alexDistrict = new E7GEZLY_API.Models.Location
        {
            SystemId = 2,
            NameEn = "Alexandria Center",
            NameAr = "وسط الإسكندرية",
            Type = "District",
            GovernorateId = 2
        };

        _context.Districts.AddRange(cairoDistrict, alexDistrict);

        // Add test venues with Egyptian context
        var venues = new List<E7GEZLY_API.Models.Venue>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "PlayStation Champions Cairo",
                VenueType = E7GEZLY_API.VenueType.PlayStationVenue,
                Features = E7GEZLY_API.VenueFeatures.PS5Available | E7GEZLY_API.VenueFeatures.WiFi | E7GEZLY_API.VenueFeatures.AirConditioning,
                Latitude = 30.0444, // Cairo coordinates
                Longitude = 31.2357,
                DistrictId = 1,
                IsActive = true,
                IsProfileComplete = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                StreetAddress = "Tahrir Square",
                Landmark = "Near Cairo Museum"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "El Ahly Football Court",
                VenueType = E7GEZLY_API.VenueType.FootballCourt,
                Features = E7GEZLY_API.VenueFeatures.LightingSystem | E7GEZLY_API.VenueFeatures.Parking,
                Latitude = 30.0626,
                Longitude = 31.2497,
                DistrictId = 1,
                IsActive = true,
                IsProfileComplete = false,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                StreetAddress = "Zamalek District",
                Landmark = "Near Al-Ahly Club"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Alexandria Gaming Lounge",
                VenueType = E7GEZLY_API.VenueType.PlayStationVenue,
                Features = E7GEZLY_API.VenueFeatures.PS4Available | E7GEZLY_API.VenueFeatures.Cafe | E7GEZLY_API.VenueFeatures.TVScreens,
                Latitude = 31.2001, // Alexandria coordinates
                Longitude = 29.9187,
                DistrictId = 2,
                IsActive = true,
                IsProfileComplete = true,
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                StreetAddress = "Corniche Road",
                Landmark = "Alexandria Library area",
                Description = "Best gaming experience in Alexandria"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Venue",
                VenueType = E7GEZLY_API.VenueType.PadelCourt,
                IsActive = false,
                IsProfileComplete = false,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-30),
                DistrictId = 1
            }
        };

        _context.Venues.AddRange(venues);

        // Add reservations for active bookings test
        var reservation = new E7GEZLY_API.Models.Reservation
        {
            Id = Guid.NewGuid(),
            VenueId = venues[0].Id, // PlayStation Champions Cairo
            BookingDate = DateTime.UtcNow.AddDays(1),
            IsCancelled = false
        };

        _context.Reservations.Add(reservation);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByTypeAsync_PlayStationVenue_ReturnsOnlyPlayStationVenues()
    {
        // Act
        var result = await _repository.GetByTypeAsync(VenueType.PlayStationVenue);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, venue => Assert.Equal(VenueType.PlayStationVenue, venue.VenueType));
        Assert.Equal(2, result.Count()); // Should return 2 active PlayStation venues
    }

    [Fact]
    public async Task GetByDistrictAsync_ValidDistrict_ReturnsVenuesInDistrict()
    {
        // Act
        var result = await _repository.GetByDistrictAsync(1); // Nasr City

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, venue => Assert.Equal(1, venue.DistrictSystemId));
        Assert.Equal(2, result.Count()); // Should return 2 venues in Nasr City (excluding inactive)
    }

    [Fact]
    public async Task SearchByNameAsync_ExactMatch_ReturnsMatchingVenue()
    {
        // Act
        var result = await _repository.SearchByNameAsync("PlayStation Champions");

        // Assert
        Assert.Single(result);
        Assert.Contains("PlayStation Champions", result.First().Name.Name);
    }

    [Fact]
    public async Task SearchByNameAsync_DescriptionMatch_ReturnsMatchingVenue()
    {
        // Act
        var result = await _repository.SearchByNameAsync("Alexandria");

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Any(v => v.Description?.Contains("Alexandria") == true || 
                                  v.Name.Name.Contains("Alexandria")));
    }

    [Fact]
    public async Task SearchByNameAsync_EmptySearchTerm_ReturnsEmptyResult()
    {
        // Act
        var result = await _repository.SearchByNameAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWithFeaturesAsync_PS5Feature_ReturnsVenuesWithPS5()
    {
        // Act
        var result = await _repository.GetWithFeaturesAsync(VenueFeatures.PS5Available);

        // Assert
        Assert.Single(result);
        Assert.True(result.First().HasFeature(VenueFeatures.PS5Available));
    }

    [Fact]
    public async Task GetWithinRadiusAsync_CairoCenter_ReturnsNearbyVenues()
    {
        // Arrange - Cairo city center coordinates
        var cairoCenter = Coordinates.Create(30.0444, 31.2357);
        var radiusKm = 10.0;

        // Act
        var result = await _repository.GetWithinRadiusAsync(cairoCenter, radiusKm);

        // Assert
        Assert.NotEmpty(result);
        // Should return Cairo venues within 10km radius
        Assert.True(result.All(v => v.Address.Coordinates != null));
    }

    [Fact]
    public async Task GetWithinRadiusAsync_InvalidRadius_ThrowsArgumentException()
    {
        // Arrange
        var center = Coordinates.Create(30.0444, 31.2357);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _repository.GetWithinRadiusAsync(center, -5));
    }

    [Fact]
    public async Task GetIncompleteProfilesAsync_ReturnsOnlyIncompleteProfiles()
    {
        // Act
        var result = await _repository.GetIncompleteProfilesAsync();

        // Assert
        Assert.Single(result); // Only 1 incomplete profile (El Ahly Football Court)
        Assert.All(result, venue => Assert.False(venue.IsProfileComplete));
    }

    [Fact]
    public async Task GetCompletedProfilesAsync_ReturnsOnlyCompleteProfiles()
    {
        // Act
        var result = await _repository.GetCompletedProfilesAsync();

        // Assert
        Assert.Equal(2, result.Count()); // 2 complete profiles
        Assert.All(result, venue => Assert.True(venue.IsProfileComplete));
    }

    [Fact]
    public async Task GetVenueCountsByTypeAsync_ReturnsCorrectCounts()
    {
        // Act
        var result = await _repository.GetVenueCountsByTypeAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result[VenueType.PlayStationVenue]); // 2 PlayStation venues
        Assert.Equal(1, result[VenueType.FootballCourt]); // 1 Football court
        Assert.Equal(0, result[VenueType.PadelCourt]); // 0 active Padel courts (1 inactive)
    }

    [Fact]
    public async Task GetProfileCompletionRateAsync_ReturnsCorrectPercentage()
    {
        // Act
        var result = await _repository.GetProfileCompletionRateAsync();

        // Assert
        // 2 completed out of 3 active venues = 67%
        Assert.Equal(67, result);
    }

    [Fact]
    public async Task GetMostPopularVenuesAsync_ReturnsCompletedVenues()
    {
        // Act
        var result = await _repository.GetMostPopularVenuesAsync(5);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count() <= 5);
        Assert.All(result, venue => Assert.True(venue.IsProfileComplete));
    }

    [Fact]
    public async Task GetVenuesByDistrictAsync_ReturnsDistrictBreakdown()
    {
        // Act
        var result = await _repository.GetVenuesByDistrictAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.ContainsKey("Nasr City"));
        Assert.True(result.ContainsKey("Alexandria Center"));
        Assert.Equal(2, result["Nasr City"]); // 2 venues in Nasr City
        Assert.Equal(1, result["Alexandria Center"]); // 1 venue in Alexandria Center
    }

    [Fact]
    public async Task HasActiveBookingsAsync_VenueWithBookings_ReturnsTrue()
    {
        // Arrange - Get the venue with a reservation
        var venueWithBooking = _context.Venues.First(v => v.Name == "PlayStation Champions Cairo");

        // Act
        var result = await _repository.HasActiveBookingsAsync(venueWithBooking.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasActiveBookingsAsync_VenueWithoutBookings_ReturnsFalse()
    {
        // Arrange - Get a venue without reservations
        var venueWithoutBooking = _context.Venues.First(v => v.Name == "El Ahly Football Court");

        // Act
        var result = await _repository.HasActiveBookingsAsync(venueWithoutBooking.Id);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}