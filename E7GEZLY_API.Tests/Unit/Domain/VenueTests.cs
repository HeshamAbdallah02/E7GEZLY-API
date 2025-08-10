using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace E7GEZLY_API.Tests.Unit.Domain
{
    /// <summary>
    /// Unit tests for Venue domain entity following Clean Architecture patterns
    /// Tests business rules and domain logic without external dependencies
    /// </summary>
    public class VenueTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldCreateVenueSuccessfully()
        {
            // Arrange
            var name = "Test Football Court";
            var venueType = VenueType.FootballCourt;
            var userEmail = "test@example.com";
            var features = VenueFeatures.WiFi;

            // Act
            var venue = Venue.Create(name, venueType, userEmail, features);

            // Assert
            venue.Should().NotBeNull();
            venue.Id.Should().NotBe(Guid.Empty);
            venue.Name.Name.Should().Be(name);
            venue.VenueType.Should().Be(venueType);
            venue.Features.Should().Be(features);
            venue.IsProfileComplete.Should().BeFalse(); // Should be false initially
            venue.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            venue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithInvalidName_ShouldThrowDomainException(string invalidName)
        {
            // Arrange
            var venueType = VenueType.FootballCourt;
            var userEmail = "test@example.com";
            var features = VenueFeatures.WiFi;

            // Act & Assert
            var action = () => Venue.Create(invalidName, venueType, userEmail, features);
            action.Should().Throw<BusinessRuleViolationException>();
        }

        [Fact]
        public void UpdateContactInfo_WithValidPhone_ShouldUpdateSuccessfully()
        {
            // Arrange
            var venue = CreateTestVenue();
            var newPhone = "+201234567890";

            // Act
            venue.UpdateContactInfo(phoneNumber: newPhone);

            // Assert
            venue.PhoneNumber.Should().Be(newPhone);
            venue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithInvalidUserEmail_ShouldNotThrow(string invalidEmail)
        {
            // Arrange
            var name = "Test Venue";
            var venueType = VenueType.FootballCourt;
            var features = VenueFeatures.WiFi;

            // Act & Assert - Should not throw as email validation is handled at application layer
            var action = () => Venue.Create(name, venueType, invalidEmail, features);
            action.Should().NotThrow();
        }

        [Fact]
        public void UpdateBasicInfo_WithValidData_ShouldUpdateSuccessfully()
        {
            // Arrange
            var venue = CreateTestVenue();
            var newName = "Updated Venue Name";
            var newFeatures = VenueFeatures.WiFi | VenueFeatures.Parking;

            // Act
            venue.UpdateBasicInfo(newName, newFeatures);

            // Assert
            venue.Name.Name.Should().Be(newName);
            venue.Features.Should().Be(newFeatures);
            venue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void MarkProfileAsComplete_WithRequiredData_ShouldMarkAsComplete()
        {
            // Arrange
            var venue = CreateTestVenue();
            venue.IsProfileComplete.Should().BeFalse(); // Initial state
            
            // Add required data for profile completion
            venue.SetWorkingHours(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
            venue.AddPricing(PricingType.MorningHour, 50m, "Test pricing");

            // Act
            venue.MarkProfileAsComplete();

            // Assert
            venue.IsProfileComplete.Should().BeTrue();
            venue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void UpdateBasicInfo_WithInvalidName_ShouldThrowBusinessRuleViolationException(string invalidName)
        {
            // Arrange
            var venue = CreateTestVenue();
            var features = VenueFeatures.WiFi;

            // Act & Assert
            var action = () => venue.UpdateBasicInfo(invalidName, features);
            action.Should().Throw<BusinessRuleViolationException>();
        }

        [Fact]
        public void UpdateContactInfo_WithDescription_ShouldUpdateSuccessfully()
        {
            // Arrange
            var venue = CreateTestVenue();
            var newDescription = "Updated venue description";

            // Act
            venue.UpdateContactInfo(description: newDescription);

            // Assert
            venue.Description.Should().Be(newDescription);
            venue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CreateExistingVenue_WithValidParameters_ShouldCreateVenueWithSpecificId()
        {
            // Arrange
            var specificId = Guid.NewGuid();
            var name = "Existing Venue";
            var venueType = VenueType.FootballCourt;
            var features = VenueFeatures.WiFi;
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var updatedAt = DateTime.UtcNow;
            var isComplete = true;

            // Act
            var venue = Venue.CreateExistingVenue(specificId, name, venueType, features,
                null, null, null, null, null, isComplete, false, createdAt, updatedAt);

            // Assert
            venue.Id.Should().Be(specificId);
            venue.Name.Name.Should().Be(name);
            venue.VenueType.Should().Be(venueType);
            venue.Features.Should().Be(features);
            venue.CreatedAt.Should().Be(createdAt);
            venue.UpdatedAt.Should().Be(updatedAt);
            venue.IsProfileComplete.Should().Be(isComplete);
        }

        [Fact]
        public void Venue_ShouldInheritFromAggregateRoot()
        {
            // Arrange & Act
            var venue = CreateTestVenue();

            // Assert
            venue.Should().BeAssignableTo<AggregateRoot>();
            venue.Id.Should().NotBe(Guid.Empty);
            venue.CreatedAt.Should().NotBe(default);
            venue.UpdatedAt.Should().NotBe(default);
        }

        [Fact]
        public void Venue_AfterUpdate_ShouldHaveUpdatedTimestamp()
        {
            // Arrange
            var venue = CreateTestVenue();
            var originalUpdatedAt = venue.UpdatedAt;
            
            // Add small delay to ensure timestamp difference
            Thread.Sleep(1);

            // Act
            venue.UpdateBasicInfo("New Name", VenueFeatures.WiFi | VenueFeatures.Parking);

            // Assert
            venue.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        /// <summary>
        /// Helper method to create a test venue with valid data
        /// </summary>
        private static Venue CreateTestVenue()
        {
            return Venue.Create(
                "Test Venue",
                VenueType.FootballCourt,
                "test@example.com",
                VenueFeatures.WiFi
            );
        }
    }
}