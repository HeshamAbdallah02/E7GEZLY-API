// DTOs/Venue/VenueProfileDtos.cs
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.User;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.DTOs.Venue
{
    public record VenueProfileResponseDto(
        string UserType,
        UserInfoDto User,
        VenueDetailsDto Venue
    );

    public record VenueDetailsDto(
        Guid Id,
        string Name,
        string Type,
        int TypeValue,
        bool IsProfileComplete,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        AddressResponseDto? Location
    );

    public record VenueSummaryDto(
        Guid Id,
        string Name,
        string Type,
        bool IsProfileComplete
    );

    /// <summary>
    /// DTO for venue profile information
    /// </summary>
    public class VenueProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VenueType { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int TypeValue { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Governorate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public bool IsProfileComplete { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AddressResponseDto? Location { get; set; }
        public List<VenueWorkingHoursDto>? WorkingHours { get; set; }
        public List<VenuePricingDto>? Pricings { get; set; }
        public List<VenuePricingDto>? Pricing { get; set; }
        public List<VenueImageDto>? Images { get; set; }
        public List<string>? ImageUrls { get; set; }
        public VenuePlayStationDetailsDto? PlayStationDetails { get; set; }
    }

    /// <summary>
    /// DTO for venue working hours
    /// </summary>
    public class VenueWorkingHoursDto
    {
        public Guid Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
        public TimeSpan? MorningStartTime { get; set; }
        public TimeSpan? MorningEndTime { get; set; }
        public TimeSpan? EveningStartTime { get; set; }
        public TimeSpan? EveningEndTime { get; set; }

        /// <summary>
        /// Check if this working hour represents a working day
        /// </summary>
        public bool IsWorkingDay() => !IsClosed;
    }

    /// <summary>
    /// DTO for venue pricing
    /// </summary>
    public class VenuePricingDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public PricingType Type { get; set; }
        public PlayStationModel? PlayStationModel { get; set; }
        public RoomType? RoomType { get; set; }
        public GameMode? GameMode { get; set; }
        public TimeSlotType? TimeSlotType { get; set; }
        public decimal? DepositPercentage { get; set; }
    }

    /// <summary>
    /// DTO for venue images
    /// </summary>
    public class VenueImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ImageType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsMainImage { get; set; }
    }

    /// <summary>
    /// DTO for PlayStation venue details
    /// </summary>
    public record VenuePlayStationDetailsDto(
        int NumberOfRooms,
        bool HasPS4,
        bool HasPS5,
        bool HasVIPRooms,
        bool HasCafe,
        bool HasWiFi,
        bool ShowsMatches
    )
    {
        // Compatibility properties for validator
        public int NumberOfConsoles => NumberOfRooms;
        public string ConsoleTypes => GetConsoleTypes();
        public bool HasPrivateRooms => HasVIPRooms;
        public int NumberOfPrivateRooms => HasVIPRooms ? NumberOfRooms : 0;

        private string GetConsoleTypes()
        {
            var types = new List<string>();
            if (HasPS4) types.Add("PS4");
            if (HasPS5) types.Add("PS5");
            return string.Join(", ", types);
        }
    };

    // Note: Enums are imported from E7GEZLY_API.Domain.Enums following Clean Architecture
}