using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.ValueObjects;

/// <summary>
/// Value object representing a physical address
/// Encapsulates address validation and formatting logic
/// </summary>
public sealed class Address : ValueObject
{
    private Address(string? streetAddress, string? landmark, Coordinates? coordinates)
    {
        StreetAddress = streetAddress;
        Landmark = landmark;
        Coordinates = coordinates;
    }

    public string? StreetAddress { get; }
    public string? Landmark { get; }
    public Coordinates? Coordinates { get; }

    public static Address Create(string? streetAddress, string? landmark, double? latitude = null, double? longitude = null)
    {
        // Validate street address length if provided
        if (!string.IsNullOrWhiteSpace(streetAddress) && streetAddress.Length > 500)
        {
            throw new BusinessRuleViolationException("Address street address cannot exceed 500 characters");
        }

        // Validate landmark length if provided
        if (!string.IsNullOrWhiteSpace(landmark) && landmark.Length > 200)
        {
            throw new BusinessRuleViolationException("Address landmark cannot exceed 200 characters");
        }

        // Create coordinates if both latitude and longitude are provided
        Coordinates? coordinates = null;
        if (latitude.HasValue && longitude.HasValue)
        {
            coordinates = Coordinates.Create(latitude.Value, longitude.Value);
        }

        return new Address(streetAddress, landmark, coordinates);
    }

    public static Address CreateEmpty()
    {
        return new Address(null, null, null);
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(StreetAddress) && string.IsNullOrWhiteSpace(Landmark) && Coordinates == null;

    public string FullAddress
    {
        get
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(StreetAddress))
                parts.Add(StreetAddress);
                
            if (!string.IsNullOrWhiteSpace(Landmark))
                parts.Add(Landmark);

            return parts.Any() ? string.Join(", ", parts) : string.Empty;
        }
    }

    public Address WithCoordinates(double latitude, double longitude)
    {
        var newCoordinates = Coordinates.Create(latitude, longitude);
        return new Address(StreetAddress, Landmark, newCoordinates);
    }

    public Address WithStreetAddress(string streetAddress)
    {
        if (streetAddress?.Length > 500)
        {
            throw new BusinessRuleViolationException("Address street address cannot exceed 500 characters");
        }

        return new Address(streetAddress, Landmark, Coordinates);
    }

    public Address WithLandmark(string landmark)
    {
        if (landmark?.Length > 200)
        {
            throw new BusinessRuleViolationException("Address landmark cannot exceed 200 characters");
        }

        return new Address(StreetAddress, landmark, Coordinates);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StreetAddress ?? string.Empty;
        yield return Landmark ?? string.Empty;
        yield return Coordinates ?? Coordinates.Empty;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(StreetAddress))
            parts.Add(StreetAddress);
            
        if (!string.IsNullOrWhiteSpace(Landmark))
            parts.Add(Landmark);

        return parts.Any() ? string.Join(", ", parts) : "No address provided";
    }
}