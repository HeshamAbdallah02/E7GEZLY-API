using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Events;
using E7GEZLY_API.Domain.ValueObjects;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Venue aggregate root - manages venue business logic and coordinates all venue-related entities
/// Represents venues in the Egyptian entertainment booking market (PlayStation, Football courts, Padel courts)
/// </summary>
public sealed class Venue : AggregateRoot
{
    private readonly List<VenueSubUser> _subUsers = new();
    private readonly List<VenueWorkingHours> _workingHours = new();
    private readonly List<VenuePricing> _pricing = new();
    private readonly List<VenueImage> _images = new();
    private readonly List<VenueAuditLog> _auditLogs = new();
    private readonly List<Reservation> _reservations = new();

    private Venue(VenueName name, VenueType venueType, string userEmail, VenueFeatures features = VenueFeatures.None, 
        Address? address = null, int? districtSystemId = null) : base()
    {
        Name = name;
        VenueType = venueType;
        Features = features;
        Address = address ?? Address.CreateEmpty();
        DistrictSystemId = districtSystemId;
        IsProfileComplete = false;
        RequiresSubUserSetup = false;
        
        AddDomainEvent(new VenueRegisteredEvent(Id, name.Name, userEmail, DateTime.UtcNow));
    }

    public static Venue Create(string name, VenueType venueType, string userEmail, 
        VenueFeatures features = VenueFeatures.None, string? streetAddress = null, string? landmark = null,
        double? latitude = null, double? longitude = null, int? districtSystemId = null)
    {
        var venueName = VenueName.Create(name);
        var address = Address.Create(streetAddress, landmark, latitude, longitude);
        
        return new Venue(venueName, venueType, userEmail, features, address, districtSystemId);
    }

    public static Venue CreateExistingVenue(Guid id, string name, VenueType venueType, VenueFeatures features,
        string? streetAddress, string? landmark, double? latitude, double? longitude, int? districtSystemId,
        bool isProfileComplete, bool requiresSubUserSetup, DateTime createdAt, DateTime updatedAt)
    {
        var venueName = VenueName.Create(name);
        var address = Address.Create(streetAddress, landmark, latitude, longitude);
        
        var venue = new Venue(venueName, venueType, string.Empty, features, address, districtSystemId);
        venue.SetId(id);
        venue.SetCreatedAt(createdAt);
        venue.SetUpdatedAt(updatedAt);
        
        // Set profile completion status
        if (isProfileComplete && !venue.IsProfileComplete)
        {
            try
            {
                venue.MarkProfileAsComplete();
            }
            catch
            {
                // If validation fails during reconstruction, force set the status
                // This is safe since we're reconstructing from persisted data
                venue.SetProfileCompletionStatusForReconstruction(isProfileComplete);
            }
        }
        
        if (requiresSubUserSetup)
        {
            venue.EnableSubUserSetup();
        }
        
        return venue;
    }

    public VenueName Name { get; private set; }
    public VenueType VenueType { get; private set; }
    public VenueFeatures Features { get; private set; }
    public Address Address { get; private set; }
    public int? DistrictSystemId { get; private set; }
    public bool IsProfileComplete { get; internal set; }
    public bool RequiresSubUserSetup { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    // Contact and social media properties
    public string? PhoneNumber { get; private set; }
    public string? WhatsAppNumber { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? InstagramUrl { get; private set; }
    public string? Description { get; private set; }
    
    public VenuePlayStationDetails? PlayStationDetails { get; private set; }

    // Navigation properties
    public District? District { get; set; }
    public IReadOnlyCollection<VenueSubUser> SubUsers => _subUsers.AsReadOnly();
    
    // Computed properties for backward compatibility
    public string? City => District?.NameEn;
    public string? Governorate => District?.Governorate?.NameEn;
    public IReadOnlyCollection<VenueWorkingHours> WorkingHours => _workingHours.AsReadOnly();
    public IReadOnlyCollection<VenuePricing> Pricing => _pricing.AsReadOnly();
    public IReadOnlyCollection<VenueImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<VenueAuditLog> AuditLogs => _auditLogs.AsReadOnly();
    public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

    public string GetFullAddress()
    {
        var addressParts = new List<string>();
        
        if (!Address.IsEmpty)
        {
            if (!string.IsNullOrWhiteSpace(Address.StreetAddress))
                addressParts.Add(Address.StreetAddress);
                
            if (!string.IsNullOrWhiteSpace(Address.Landmark))
                addressParts.Add(Address.Landmark);
        }

        if (District != null)
        {
            addressParts.Add(District.NameEn);
            if (District.Governorate != null)
                addressParts.Add(District.Governorate.NameEn);
        }

        return addressParts.Any() ? string.Join(", ", addressParts) : "No address provided";
    }

    // Venue management methods
    public void UpdateBasicInfo(string name, VenueFeatures features)
    {
        var newName = VenueName.Create(name);
        
        if (Name != newName || Features != features)
        {
            Name = newName;
            Features = features;
            MarkAsUpdated();
            
            AddDomainEvent(new VenueDetailsUpdatedEvent(Id, name, null, DateTime.UtcNow));
        }
    }
    
    public void UpdateContactInfo(string? phoneNumber = null, string? whatsAppNumber = null, 
        string? facebookUrl = null, string? instagramUrl = null, string? description = null)
    {
        var hasChanges = false;
        
        if (phoneNumber != PhoneNumber)
        {
            PhoneNumber = phoneNumber;
            hasChanges = true;
        }
        
        if (whatsAppNumber != WhatsAppNumber)
        {
            WhatsAppNumber = whatsAppNumber;
            hasChanges = true;
        }
        
        if (facebookUrl != FacebookUrl)
        {
            FacebookUrl = facebookUrl;
            hasChanges = true;
        }
        
        if (instagramUrl != InstagramUrl)
        {
            InstagramUrl = instagramUrl;
            hasChanges = true;
        }
        
        if (description != Description)
        {
            Description = description;
            hasChanges = true;
        }
        
        if (hasChanges)
        {
            MarkAsUpdated();
            AddDomainEvent(new VenueDetailsUpdatedEvent(Id, Name.Name, null, DateTime.UtcNow));
        }
    }
    
    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            MarkAsUpdated();
        }
    }
    
    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            MarkAsUpdated();
        }
    }

    public void UpdateAddress(string? streetAddress, string? landmark, double? latitude = null, double? longitude = null)
    {
        var newAddress = Address.Create(streetAddress, landmark, latitude, longitude);
        
        if (Address != newAddress)
        {
            Address = newAddress;
            MarkAsUpdated();
        }
    }

    public void UpdateDistrict(int? districtSystemId)
    {
        if (DistrictSystemId != districtSystemId)
        {
            DistrictSystemId = districtSystemId;
            MarkAsUpdated();
        }
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        Address = Address.WithCoordinates(latitude, longitude);
        MarkAsUpdated();
    }

    // PlayStation details management
    public void SetPlayStationDetails(int numberOfRooms, bool hasPS4, bool hasPS5, bool hasVIPRooms,
        bool hasCafe = false, bool hasWiFi = false, bool showsMatches = false)
    {
        if (VenueType != VenueType.PlayStationVenue && VenueType != VenueType.MultiPurpose)
            throw new BusinessRuleViolationException("PlayStation details can only be set for PlayStation venues or multi-purpose venues");

        if (numberOfRooms <= 0)
            throw new BusinessRuleViolationException("Number of rooms must be greater than zero");

        if (!hasPS4 && !hasPS5)
            throw new BusinessRuleViolationException("Venue must have at least one PlayStation model available");

        PlayStationDetails = VenuePlayStationDetails.Create(Id, numberOfRooms, hasPS4, hasPS5, hasVIPRooms, hasCafe, hasWiFi, showsMatches);
        MarkAsUpdated();
    }

    // Working hours management
    public void SetWorkingHours(DayOfWeek dayOfWeek, TimeSpan openTime, TimeSpan closeTime, bool isClosed = false,
        TimeSpan? morningStart = null, TimeSpan? morningEnd = null, TimeSpan? eveningStart = null, TimeSpan? eveningEnd = null)
    {
        if (openTime >= closeTime && !isClosed)
            throw new BusinessRuleViolationException("Open time must be before close time unless the venue is closed");

        // Remove existing working hours for this day
        var existingHours = _workingHours.Where(wh => wh.DayOfWeek == dayOfWeek).ToList();
        foreach (var hours in existingHours)
        {
            _workingHours.Remove(hours);
        }

        // Add new working hours
        var workingHours = VenueWorkingHours.Create(Id, dayOfWeek, openTime, closeTime, isClosed, 
            morningStart, morningEnd, eveningStart, eveningEnd);
        _workingHours.Add(workingHours);
        
        MarkAsUpdated();
        AddDomainEvent(new VenueWorkingHoursUpdatedEvent(Id, null, DateTime.UtcNow));
    }

    // Pricing management
    public void AddPricing(PricingType type, decimal price, string? description = null,
        PlayStationModel? psModel = null, RoomType? roomType = null, GameMode? gameMode = null,
        TimeSlotType? timeSlotType = null, decimal? depositPercentage = null)
    {
        if (price <= 0)
            throw new BusinessRuleViolationException("Price must be greater than zero");

        if (depositPercentage.HasValue && (depositPercentage < 0 || depositPercentage > 100))
            throw new BusinessRuleViolationException("Deposit percentage must be between 0 and 100");

        var pricing = VenuePricing.Create(Id, type, price, description, psModel, roomType, gameMode, timeSlotType, depositPercentage);
        _pricing.Add(pricing);
        
        MarkAsUpdated();
        AddDomainEvent(new VenuePricingUpdatedEvent(Id, null, DateTime.UtcNow));
    }

    public void RemovePricing(Guid pricingId)
    {
        var pricing = _pricing.FirstOrDefault(p => p.Id == pricingId);
        if (pricing != null)
        {
            _pricing.Remove(pricing);
            MarkAsUpdated();
            AddDomainEvent(new VenuePricingUpdatedEvent(Id, null, DateTime.UtcNow));
        }
    }

    // Image management
    public void AddImage(string imageUrl, string? caption = null, int displayOrder = 0, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new BusinessRuleViolationException("Image URL is required");

        if (imageUrl.Length > 500)
            throw new BusinessRuleViolationException("Image URL cannot exceed 500 characters");

        // If setting as primary, remove primary flag from other images
        if (isPrimary)
        {
            foreach (var img in _images.Where(i => i.IsPrimary))
            {
                img.SetAsPrimary(false);
            }
        }

        var image = VenueImage.Create(Id, imageUrl, caption, displayOrder, isPrimary);
        _images.Add(image);
        MarkAsUpdated();
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            _images.Remove(image);
            MarkAsUpdated();
        }
    }

    // Sub-user management
    public VenueSubUser CreateFounderAdmin(string username, string passwordHash)
    {
        if (_subUsers.Any(su => su.IsFounderAdmin))
            throw new BusinessRuleViolationException("Founder admin already exists for this venue");

        var founderAdmin = VenueSubUser.CreateFounderAdmin(Id, username, passwordHash);
        _subUsers.Add(founderAdmin);
        
        MarkAsUpdated();
        AddDomainEvent(new VenueSubUserCreatedEvent(Id, founderAdmin.Id, username, "FounderAdmin", null, DateTime.UtcNow));
        
        return founderAdmin;
    }

    public VenueSubUser CreateSubUser(string username, string passwordHash, VenueSubUserRole role, 
        VenuePermissions permissions, Guid createdBySubUserId)
    {
        // Validate that the creator exists and has permission
        var creator = _subUsers.FirstOrDefault(su => su.Id == createdBySubUserId && su.IsActive);
        if (creator == null)
            throw new BusinessRuleViolationException("Creator sub-user not found or inactive");

        if (!creator.HasPermission(VenuePermissions.CreateSubUsers) && !creator.IsFounderAdmin)
            throw new BusinessRuleViolationException("Creator does not have permission to create sub-users");

        // Check for duplicate username within venue
        if (_subUsers.Any(su => su.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && su.IsActive))
            throw new BusinessRuleViolationException("Username already exists in this venue");

        var subUser = VenueSubUser.Create(Id, username, passwordHash, role, permissions, createdBySubUserId);
        _subUsers.Add(subUser);
        
        MarkAsUpdated();
        AddDomainEvent(new VenueSubUserCreatedEvent(Id, subUser.Id, username, role.ToString(), createdBySubUserId, DateTime.UtcNow));
        
        LogAuditEvent("SubUser.Created", "VenueSubUser", subUser.Id.ToString(), null, 
            $"Username: {username}, Role: {role}", createdBySubUserId);
        
        return subUser;
    }

    public void UpdateSubUser(Guid subUserId, VenueSubUserRole? role, VenuePermissions? permissions, Guid updatedBySubUserId)
    {
        var subUser = _subUsers.FirstOrDefault(su => su.Id == subUserId);
        if (subUser == null)
            throw new DomainEntityNotFoundException("VenueSubUser", subUserId);

        var updater = _subUsers.FirstOrDefault(su => su.Id == updatedBySubUserId && su.IsActive);
        if (updater == null)
            throw new BusinessRuleViolationException("Updater sub-user not found or inactive");

        if (!updater.HasPermission(VenuePermissions.EditSubUsers) && !updater.IsFounderAdmin)
            throw new BusinessRuleViolationException("Updater does not have permission to edit sub-users");

        var oldValues = $"Role: {subUser.Role}, Permissions: {subUser.Permissions}";
        
        if (role.HasValue)
            subUser.UpdateRole(role.Value);
            
        if (permissions.HasValue)
            subUser.UpdatePermissions(permissions.Value);
        
        var newValues = $"Role: {subUser.Role}, Permissions: {subUser.Permissions}";
        
        MarkAsUpdated();
        AddDomainEvent(new VenueSubUserUpdatedEvent(Id, subUserId, subUser.Username, updatedBySubUserId, DateTime.UtcNow));
        
        LogAuditEvent("SubUser.Updated", "VenueSubUser", subUserId.ToString(), oldValues, newValues, updatedBySubUserId);
    }

    public void DeactivateSubUser(Guid subUserId, Guid deactivatedBySubUserId)
    {
        var subUser = _subUsers.FirstOrDefault(su => su.Id == subUserId);
        if (subUser == null)
            throw new DomainEntityNotFoundException("VenueSubUser", subUserId);

        if (subUser.IsFounderAdmin)
            throw new BusinessRuleViolationException("Cannot deactivate founder admin");

        var deactivator = _subUsers.FirstOrDefault(su => su.Id == deactivatedBySubUserId && su.IsActive);
        if (deactivator == null)
            throw new BusinessRuleViolationException("Deactivator sub-user not found or inactive");

        if (!deactivator.HasPermission(VenuePermissions.DeleteSubUsers) && !deactivator.IsFounderAdmin)
            throw new BusinessRuleViolationException("Deactivator does not have permission to deactivate sub-users");

        subUser.Deactivate();
        
        MarkAsUpdated();
        AddDomainEvent(new VenueSubUserDeactivatedEvent(Id, subUserId, subUser.Username, deactivatedBySubUserId, DateTime.UtcNow));
        
        LogAuditEvent("SubUser.Deactivated", "VenueSubUser", subUserId.ToString(), null, "Deactivated", deactivatedBySubUserId);
    }

    public VenueSubUser? FindSubUser(string username)
    {
        return _subUsers.FirstOrDefault(su => su.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && su.IsActive);
    }

    public VenueSubUser? FindSubUser(Guid subUserId)
    {
        return _subUsers.FirstOrDefault(su => su.Id == subUserId && su.IsActive);
    }

    // Profile completion
    public void MarkProfileAsComplete()
    {
        if (IsProfileComplete) return;
        
        // Validate profile completeness
        if (string.IsNullOrWhiteSpace(Name.Name))
            throw new BusinessRuleViolationException("Venue name is required to complete profile");

        if (!_workingHours.Any())
            throw new BusinessRuleViolationException("Working hours must be set to complete profile");

        if (!_pricing.Any())
            throw new BusinessRuleViolationException("Pricing must be set to complete profile");

        // Additional validations based on venue type
        if (VenueType == VenueType.PlayStationVenue && PlayStationDetails == null)
            throw new BusinessRuleViolationException("PlayStation details must be set to complete profile for PlayStation venues");

        IsProfileComplete = true;
        MarkAsUpdated();
        
        AddDomainEvent(new VenueProfileCompletedEvent(Id, Name.Name, DateTime.UtcNow));
    }

    public void EnableSubUserSetup()
    {
        RequiresSubUserSetup = true;
        MarkAsUpdated();
    }

    public void SetRequiresSubUserSetup(bool requiresSetup)
    {
        RequiresSubUserSetup = requiresSetup;
        MarkAsUpdated();
    }

    // Audit logging
    public void LogAuditEvent(string action, string entityType, string entityId, string? oldValues, 
        string? newValues, Guid? subUserId, string? ipAddress = null, string? userAgent = null, string? additionalData = null)
    {
        var auditLog = VenueAuditLog.Create(Id, action, entityType, entityId, oldValues, newValues, 
            subUserId, ipAddress, userAgent, additionalData);
        _auditLogs.Add(auditLog);
        MarkAsUpdated();
    }

    // Distance and location methods
    public double? GetDistanceTo(Coordinates targetCoordinates)
    {
        return Address.Coordinates?.DistanceTo(targetCoordinates);
    }

    public bool IsInEgypt()
    {
        return Address.Coordinates?.IsWithinEgypt() ?? false;
    }

    public bool HasFeature(VenueFeatures feature)
    {
        return (Features & feature) == feature;
    }

    // Internal method for reconstruction from persistence layer
    internal void SetProfileCompletionStatusForReconstruction(bool isComplete)
    {
        IsProfileComplete = isComplete;
    }

    public override string ToString()
    {
        return $"{Name.Name} ({VenueType}) - {GetFullAddress()}";
    }
}