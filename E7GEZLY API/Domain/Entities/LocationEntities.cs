using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.ValueObjects;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity representing a Governorate (محافظة) in Egypt
/// This is a read-only reference entity managed by the system
/// </summary>
public sealed class Governorate : BaseEntity
{
    private readonly List<District> _districts = new();

    private Governorate(int id, string nameEn, string nameAr) : base(Guid.NewGuid())
    {
        SystemId = id;
        NameEn = nameEn;
        NameAr = nameAr;
    }

    // Factory method for creating governorates (typically used by data seeding)
    public static Governorate Create(int systemId, string nameEn, string nameAr)
    {
        if (systemId <= 0)
            throw new BusinessRuleViolationException("Governorate system ID must be positive");

        if (string.IsNullOrWhiteSpace(nameEn))
            throw new BusinessRuleViolationException("Governorate English name is required");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleViolationException("Governorate Arabic name is required");

        if (nameEn.Length > 100)
            throw new BusinessRuleViolationException("Governorate English name cannot exceed 100 characters");

        if (nameAr.Length > 100)
            throw new BusinessRuleViolationException("Governorate Arabic name cannot exceed 100 characters");

        return new Governorate(systemId, nameEn.Trim(), nameAr.Trim());
    }

    public static Governorate CreateExisting(Guid id, int systemId, string nameEn, string nameAr, DateTime createdAt, DateTime updatedAt)
    {
        if (systemId <= 0)
            throw new BusinessRuleViolationException("Governorate system ID must be positive");

        if (string.IsNullOrWhiteSpace(nameEn))
            throw new BusinessRuleViolationException("Governorate English name is required");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleViolationException("Governorate Arabic name is required");

        var governorate = new Governorate(systemId, nameEn.Trim(), nameAr.Trim());
        governorate.SetId(id);
        governorate.SetCreatedAt(createdAt);
        governorate.SetUpdatedAt(updatedAt);

        return governorate;
    }

    public int SystemId { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public IReadOnlyCollection<District> Districts => _districts.AsReadOnly();

    internal void AddDistrict(District district)
    {
        if (_districts.Any(d => d.SystemId == district.SystemId))
            throw new BusinessRuleViolationException($"District with system ID {district.SystemId} already exists in governorate {NameEn}");

        _districts.Add(district);
    }

    public District? FindDistrict(int districtSystemId)
    {
        return _districts.FirstOrDefault(d => d.SystemId == districtSystemId);
    }

    public override string ToString()
    {
        return $"{NameEn} ({NameAr})";
    }
}

/// <summary>
/// Domain entity representing a District (حي/منطقة) within a Governorate
/// This is a read-only reference entity managed by the system
/// </summary>
public sealed class District : BaseEntity
{
    private District(int systemId, string nameEn, string nameAr, int governorateId, Coordinates? centerCoordinates = null) : base(Guid.NewGuid())
    {
        SystemId = systemId;
        NameEn = nameEn;
        NameAr = nameAr;
        GovernorateSystemId = governorateId;
        CenterCoordinates = centerCoordinates;
    }

    // Factory method for creating districts (typically used by data seeding)
    public static District Create(int systemId, string nameEn, string nameAr, int governorateSystemId, 
        double? centerLatitude = null, double? centerLongitude = null)
    {
        if (systemId <= 0)
            throw new BusinessRuleViolationException("District system ID must be positive");

        if (string.IsNullOrWhiteSpace(nameEn))
            throw new BusinessRuleViolationException("District English name is required");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleViolationException("District Arabic name is required");

        if (governorateSystemId <= 0)
            throw new BusinessRuleViolationException("Governorate system ID must be positive");

        if (nameEn.Length > 100)
            throw new BusinessRuleViolationException("District English name cannot exceed 100 characters");

        if (nameAr.Length > 100)
            throw new BusinessRuleViolationException("District Arabic name cannot exceed 100 characters");

        Coordinates? centerCoordinates = null;
        if (centerLatitude.HasValue && centerLongitude.HasValue)
        {
            centerCoordinates = Coordinates.Create(centerLatitude.Value, centerLongitude.Value);
        }

        return new District(systemId, nameEn.Trim(), nameAr.Trim(), governorateSystemId, centerCoordinates);
    }

    public static District CreateExisting(Guid id, int systemId, string nameEn, string nameAr, int governorateSystemId,
        double? centerLatitude, double? centerLongitude, DateTime createdAt, DateTime updatedAt)
    {
        if (systemId <= 0)
            throw new BusinessRuleViolationException("District system ID must be positive");

        if (string.IsNullOrWhiteSpace(nameEn))
            throw new BusinessRuleViolationException("District English name is required");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleViolationException("District Arabic name is required");

        if (governorateSystemId <= 0)
            throw new BusinessRuleViolationException("Governorate system ID must be positive");

        Coordinates? centerCoordinates = null;
        if (centerLatitude.HasValue && centerLongitude.HasValue)
        {
            centerCoordinates = Coordinates.Create(centerLatitude.Value, centerLongitude.Value);
        }

        var district = new District(systemId, nameEn.Trim(), nameAr.Trim(), governorateSystemId, centerCoordinates);
        district.SetId(id);
        district.SetCreatedAt(createdAt);
        district.SetUpdatedAt(updatedAt);

        return district;
    }

    public int SystemId { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public int GovernorateSystemId { get; private set; }
    public Coordinates? CenterCoordinates { get; private set; }
    
    // Property for compatibility with existing code
    public int GovernorateId => GovernorateSystemId;
    
    // Navigation property set by the aggregate root
    public Governorate? Governorate { get; internal set; }

    public string GetDisplayName(bool useArabic = false)
    {
        return useArabic ? NameAr : NameEn;
    }

    public string GetFullDisplayName(bool useArabic = false)
    {
        var districtName = GetDisplayName(useArabic);
        var governorateName = Governorate?.GetDisplayName(useArabic) ?? (useArabic ? "غير محدد" : "Unknown");
        return $"{districtName}, {governorateName}";
    }

    private string GetDisplayName(bool useArabic, Governorate? governorate)
    {
        return useArabic ? (governorate?.NameAr ?? "غير محدد") : (governorate?.NameEn ?? "Unknown");
    }

    public override string ToString()
    {
        return GetFullDisplayName();
    }
}

// Extension method for Governorate to get display name
public static class GovernorateExtensions
{
    public static string GetDisplayName(this Governorate governorate, bool useArabic = false)
    {
        return useArabic ? governorate.NameAr : governorate.NameEn;
    }
}