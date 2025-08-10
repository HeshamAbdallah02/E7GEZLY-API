using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity representing working hours for a venue
/// Supports different time slots for court-based venues (morning/evening)
/// </summary>
public sealed class VenueWorkingHours : BaseEntity
{
    private VenueWorkingHours(Guid venueId, DayOfWeek dayOfWeek, TimeSpan openTime, TimeSpan closeTime, 
        bool isClosed, TimeSpan? morningStart, TimeSpan? morningEnd, TimeSpan? eveningStart, TimeSpan? eveningEnd) : base()
    {
        VenueId = venueId;
        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;
        MorningStartTime = morningStart;
        MorningEndTime = morningEnd;
        EveningStartTime = eveningStart;
        EveningEndTime = eveningEnd;
    }

    public static VenueWorkingHours Create(Guid venueId, DayOfWeek dayOfWeek, TimeSpan openTime, TimeSpan closeTime, 
        bool isClosed = false, TimeSpan? morningStart = null, TimeSpan? morningEnd = null, 
        TimeSpan? eveningStart = null, TimeSpan? eveningEnd = null)
    {
        if (venueId == Guid.Empty)
            throw new BusinessRuleViolationException("Venue ID is required for working hours");

        if (!isClosed && openTime >= closeTime)
            throw new BusinessRuleViolationException("Open time must be before close time unless venue is closed");

        // Validate morning/evening slots if provided
        if (morningStart.HasValue && morningEnd.HasValue && morningStart >= morningEnd)
            throw new BusinessRuleViolationException("Morning start time must be before morning end time");

        if (eveningStart.HasValue && eveningEnd.HasValue && eveningStart >= eveningEnd)
            throw new BusinessRuleViolationException("Evening start time must be before evening end time");

        // Validate that morning ends before evening starts
        if (morningEnd.HasValue && eveningStart.HasValue && morningEnd >= eveningStart)
            throw new BusinessRuleViolationException("Morning slot must end before evening slot starts");

        return new VenueWorkingHours(venueId, dayOfWeek, openTime, closeTime, isClosed, 
            morningStart, morningEnd, eveningStart, eveningEnd);
    }

    public Guid VenueId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeSpan OpenTime { get; private set; }
    public TimeSpan CloseTime { get; private set; }
    public bool IsClosed { get; private set; }
    public TimeSpan? MorningStartTime { get; private set; }
    public TimeSpan? MorningEndTime { get; private set; }
    public TimeSpan? EveningStartTime { get; private set; }
    public TimeSpan? EveningEndTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    public bool HasTimeSlots => MorningStartTime.HasValue && EveningStartTime.HasValue;

    public bool IsOperatingAt(TimeSpan time)
    {
        if (IsClosed) return false;
        return time >= OpenTime && time <= CloseTime;
    }

    public bool IsMorningSlot(TimeSpan time)
    {
        if (!MorningStartTime.HasValue || !MorningEndTime.HasValue) return false;
        return time >= MorningStartTime.Value && time < MorningEndTime.Value;
    }

    public bool IsEveningSlot(TimeSpan time)
    {
        if (!EveningStartTime.HasValue || !EveningEndTime.HasValue) return false;
        return time >= EveningStartTime.Value && time < EveningEndTime.Value;
    }

    public void Update(TimeSpan openTime, TimeSpan closeTime, bool isClosed = false,
        TimeSpan? morningStart = null, TimeSpan? morningEnd = null, 
        TimeSpan? eveningStart = null, TimeSpan? eveningEnd = null)
    {
        if (!isClosed && openTime >= closeTime)
            throw new BusinessRuleViolationException("Open time must be before close time unless venue is closed");

        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;
        MorningStartTime = morningStart;
        MorningEndTime = morningEnd;
        EveningStartTime = eveningStart;
        EveningEndTime = eveningEnd;
        MarkAsUpdated();
    }

    public override string ToString()
    {
        if (IsClosed) return $"{DayOfWeek}: Closed";
        return $"{DayOfWeek}: {OpenTime:hh\\:mm} - {CloseTime:hh\\:mm}";
    }
}

/// <summary>
/// Domain entity representing pricing structure for venues
/// Supports different pricing models for PlayStation venues and courts
/// </summary>
public sealed class VenuePricing : BaseEntity
{
    private VenuePricing(Guid venueId, PricingType type, decimal price, string? description,
        PlayStationModel? psModel, RoomType? roomType, GameMode? gameMode, 
        TimeSlotType? timeSlotType, decimal? depositPercentage) : base()
    {
        VenueId = venueId;
        Type = type;
        Price = price;
        Description = description;
        PlayStationModel = psModel;
        RoomType = roomType;
        GameMode = gameMode;
        TimeSlotType = timeSlotType;
        DepositPercentage = depositPercentage;
    }

    public static VenuePricing Create(Guid venueId, PricingType type, decimal price, string? description = null,
        PlayStationModel? psModel = null, RoomType? roomType = null, GameMode? gameMode = null,
        TimeSlotType? timeSlotType = null, decimal? depositPercentage = null)
    {
        if (venueId == Guid.Empty)
            throw new BusinessRuleViolationException("Venue ID is required for pricing");

        if (price <= 0)
            throw new BusinessRuleViolationException("Price must be greater than zero");

        if (depositPercentage.HasValue && (depositPercentage < 0 || depositPercentage > 100))
            throw new BusinessRuleViolationException("Deposit percentage must be between 0 and 100");

        if (description?.Length > 200)
            throw new BusinessRuleViolationException("Pricing description cannot exceed 200 characters");

        return new VenuePricing(venueId, type, price, description, psModel, roomType, gameMode, timeSlotType, depositPercentage);
    }

    public Guid VenueId { get; private set; }
    public PricingType Type { get; private set; }
    public decimal Price { get; private set; }
    public string? Description { get; private set; }
    public PlayStationModel? PlayStationModel { get; private set; }
    public RoomType? RoomType { get; private set; }
    public GameMode? GameMode { get; private set; }
    public TimeSlotType? TimeSlotType { get; private set; }
    public decimal? DepositPercentage { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Properties to match DTO expectations
    public string Name => GeneratePricingName();
    public decimal PricePerHour => Price;

    public decimal? DepositAmount => DepositPercentage.HasValue ? Price * (DepositPercentage.Value / 100) : null;

    public void UpdatePrice(decimal price)
    {
        if (price <= 0)
            throw new BusinessRuleViolationException("Price must be greater than zero");

        Price = price;
        MarkAsUpdated();
    }

    public void UpdateDescription(string? description)
    {
        if (description?.Length > 200)
            throw new BusinessRuleViolationException("Pricing description cannot exceed 200 characters");

        Description = description;
        MarkAsUpdated();
    }

    public void UpdateDepositPercentage(decimal? depositPercentage)
    {
        if (depositPercentage.HasValue && (depositPercentage < 0 || depositPercentage > 100))
            throw new BusinessRuleViolationException("Deposit percentage must be between 0 and 100");

        DepositPercentage = depositPercentage;
        MarkAsUpdated();
    }

    private string GeneratePricingName()
    {
        var nameParts = new List<string> { Type.ToString() };
        
        if (PlayStationModel.HasValue) nameParts.Add($"PS{(int)PlayStationModel.Value}");
        if (RoomType.HasValue) nameParts.Add(RoomType.Value.ToString());
        if (GameMode.HasValue) nameParts.Add(GameMode.Value.ToString());
        if (TimeSlotType.HasValue) nameParts.Add(TimeSlotType.Value.ToString());
        
        return string.Join(" - ", nameParts);
    }

    public override string ToString()
    {
        var details = new List<string>();
        
        if (PlayStationModel.HasValue) details.Add($"PS{(int)PlayStationModel.Value}");
        if (RoomType.HasValue) details.Add(RoomType.Value.ToString());
        if (GameMode.HasValue) details.Add(GameMode.Value.ToString());
        if (TimeSlotType.HasValue) details.Add(TimeSlotType.Value.ToString());

        var detailsStr = details.Any() ? $" ({string.Join(", ", details)})" : "";
        return $"{Type}{detailsStr}: {Price:C}";
    }
}

/// <summary>
/// Domain entity representing images uploaded for venues
/// </summary>
public sealed class VenueImage : BaseEntity
{
    private VenueImage(Guid venueId, string imageUrl, string? caption, int displayOrder, bool isPrimary) : base()
    {
        VenueId = venueId;
        ImageUrl = imageUrl;
        Caption = caption;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    public static VenueImage Create(Guid venueId, string imageUrl, string? caption = null, int displayOrder = 0, bool isPrimary = false)
    {
        if (venueId == Guid.Empty)
            throw new BusinessRuleViolationException("Venue ID is required for image");

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new BusinessRuleViolationException("Image URL is required");

        if (imageUrl.Length > 500)
            throw new BusinessRuleViolationException("Image URL cannot exceed 500 characters");

        if (caption?.Length > 200)
            throw new BusinessRuleViolationException("Image caption cannot exceed 200 characters");

        return new VenueImage(venueId, imageUrl, caption, displayOrder, isPrimary);
    }

    public Guid VenueId { get; private set; }
    public string ImageUrl { get; private set; }
    public string? Caption { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void UpdateCaption(string? caption)
    {
        if (caption?.Length > 200)
            throw new BusinessRuleViolationException("Image caption cannot exceed 200 characters");

        Caption = caption;
        MarkAsUpdated();
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        MarkAsUpdated();
    }

    public void SetAsPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        MarkAsUpdated();
    }

    public override string ToString()
    {
        var primaryStr = IsPrimary ? " (Primary)" : "";
        return $"Image {DisplayOrder}{primaryStr}: {ImageUrl}";
    }
}

/// <summary>
/// PlayStation-specific venue details
/// </summary>
public sealed class VenuePlayStationDetails : BaseEntity
{
    private VenuePlayStationDetails(Guid venueId, int numberOfRooms, bool hasPS4, bool hasPS5, 
        bool hasVIPRooms, bool hasCafe, bool hasWiFi, bool showsMatches) : base()
    {
        VenueId = venueId;
        NumberOfRooms = numberOfRooms;
        HasPS4 = hasPS4;
        HasPS5 = hasPS5;
        HasVIPRooms = hasVIPRooms;
        HasCafe = hasCafe;
        HasWiFi = hasWiFi;
        ShowsMatches = showsMatches;
    }

    public static VenuePlayStationDetails Create(Guid venueId, int numberOfRooms, bool hasPS4, bool hasPS5, 
        bool hasVIPRooms, bool hasCafe = false, bool hasWiFi = false, bool showsMatches = false)
    {
        if (venueId == Guid.Empty)
            throw new BusinessRuleViolationException("Venue ID is required for PlayStation details");

        if (numberOfRooms <= 0)
            throw new BusinessRuleViolationException("Number of rooms must be greater than zero");

        if (!hasPS4 && !hasPS5)
            throw new BusinessRuleViolationException("Venue must have at least one PlayStation model available");

        return new VenuePlayStationDetails(venueId, numberOfRooms, hasPS4, hasPS5, hasVIPRooms, hasCafe, hasWiFi, showsMatches);
    }

    public Guid VenueId { get; private set; }
    public int NumberOfRooms { get; private set; }
    public bool HasPS4 { get; private set; }
    public bool HasPS5 { get; private set; }
    public bool HasVIPRooms { get; private set; }
    public bool HasCafe { get; private set; }
    public bool HasWiFi { get; private set; }
    public bool ShowsMatches { get; private set; }

    public void UpdateRoomCount(int numberOfRooms)
    {
        if (numberOfRooms <= 0)
            throw new BusinessRuleViolationException("Number of rooms must be greater than zero");

        NumberOfRooms = numberOfRooms;
        MarkAsUpdated();
    }

    public void UpdatePlayStationModels(bool hasPS4, bool hasPS5)
    {
        if (!hasPS4 && !hasPS5)
            throw new BusinessRuleViolationException("Venue must have at least one PlayStation model available");

        HasPS4 = hasPS4;
        HasPS5 = hasPS5;
        MarkAsUpdated();
    }

    public void UpdateFeatures(bool hasVIPRooms, bool hasCafe, bool hasWiFi, bool showsMatches)
    {
        HasVIPRooms = hasVIPRooms;
        HasCafe = hasCafe;
        HasWiFi = hasWiFi;
        ShowsMatches = showsMatches;
        MarkAsUpdated();
    }

    public List<string> GetAvailableFeatures()
    {
        var features = new List<string>();
        
        if (HasPS4) features.Add("PlayStation 4");
        if (HasPS5) features.Add("PlayStation 5");
        if (HasVIPRooms) features.Add("VIP Rooms");
        if (HasCafe) features.Add("Café");
        if (HasWiFi) features.Add("Wi-Fi");
        if (ShowsMatches) features.Add("Live Sports");

        return features;
    }

    public override string ToString()
    {
        return $"{NumberOfRooms} rooms, {string.Join(", ", GetAvailableFeatures())}";
    }
}