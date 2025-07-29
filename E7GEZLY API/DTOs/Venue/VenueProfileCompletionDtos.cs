// DTOs/Venue/VenueProfileCompletionDtos.cs
using System.ComponentModel.DataAnnotations;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.DTOs.Venue
{
    /// <summary>
    /// Base DTO for venue profile completion
    /// </summary>
    public abstract record CompleteVenueProfileBaseDto
    {
        [Required]
        public double Latitude { get; init; }

        [Required]
        public double Longitude { get; init; }

        [Required]
        public int DistrictId { get; init; }

        [StringLength(500)]
        public string? StreetAddress { get; init; }

        [StringLength(200)]
        public string? Landmark { get; init; }

        [Required]
        public List<WorkingHoursDto> WorkingHours { get; init; } = new();

        public List<string>? ImageUrls { get; init; }
    }

    /// <summary>
    /// DTO for completing court venue profiles (Football/Padel)
    /// </summary>
    public record CompleteCourtProfileDto : CompleteVenueProfileBaseDto
    {
        [Required]
        public TimeSpan MorningStartTime { get; init; }

        [Required]
        public TimeSpan MorningEndTime { get; init; }

        [Required]
        public TimeSpan EveningStartTime { get; init; }

        [Required]
        public TimeSpan EveningEndTime { get; init; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal MorningHourPrice { get; init; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal EveningHourPrice { get; init; }

        [Required]
        [Range(0, 100)]
        public decimal DepositPercentage { get; init; } = 25;
    }

    /// <summary>
    /// DTO for completing PlayStation venue profiles
    /// </summary>
    public record CompletePlayStationProfileDto : CompleteVenueProfileBaseDto
    {
        [Required]
        [Range(1, 1000)]
        public int NumberOfRooms { get; init; }

        [Required]
        public bool HasPS4 { get; init; }

        [Required]
        public bool HasPS5 { get; init; }

        public bool HasVIPRooms { get; init; }

        public PlayStationPricingDto? PS4Pricing { get; init; }
        public PlayStationPricingDto? PS5Pricing { get; init; }

        // Features
        public bool HasCafe { get; init; }
        public bool HasWiFi { get; init; }
        public bool ShowsMatches { get; init; }
    }

    /// <summary>
    /// PlayStation pricing structure
    /// </summary>
    public record PlayStationPricingDto
    {
        public RoomPricingDto? ClassicRooms { get; init; }
        public RoomPricingDto? VIPRooms { get; init; }
    }

    /// <summary>
    /// Room pricing for single/multiplayer modes
    /// </summary>
    public record RoomPricingDto
    {
        [Required]
        [Range(0.01, 999999.99)]
        public decimal SingleModeHourPrice { get; init; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal MultiplayerModeHourPrice { get; init; }
    }

    /// <summary>
    /// Working hours DTO
    /// </summary>
    public record WorkingHoursDto
    {
        [Required]
        public DayOfWeek DayOfWeek { get; init; }

        public bool IsClosed { get; init; } = false;

        public TimeSpan? OpenTime { get; init; }
        public TimeSpan? CloseTime { get; init; }
    }

    /// <summary>
    /// Response DTO for completed venue profile
    /// </summary>
    public record VenueProfileCompletionResponseDto(
        string Message,
        bool IsProfileComplete,
        VenueDetailsDto Venue
    );
}