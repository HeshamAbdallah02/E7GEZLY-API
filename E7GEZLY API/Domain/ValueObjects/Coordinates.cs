using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.ValueObjects;

/// <summary>
/// Value object representing geographic coordinates
/// Ensures latitude and longitude values are within valid ranges
/// </summary>
public sealed class Coordinates : ValueObject
{
    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    public static Coordinates Create(double latitude, double longitude)
    {
        // Validate latitude range (-90 to 90)
        if (latitude < -90 || latitude > 90)
        {
            throw new BusinessRuleViolationException($"Latitude must be between -90 and 90 degrees, but was {latitude}");
        }

        // Validate longitude range (-180 to 180)
        if (longitude < -180 || longitude > 180)
        {
            throw new BusinessRuleViolationException($"Longitude must be between -180 and 180 degrees, but was {longitude}");
        }

        return new Coordinates(latitude, longitude);
    }

    public static Coordinates Empty => new(0, 0);

    /// <summary>
    /// Calculate the distance between two coordinates using the Haversine formula
    /// Returns the distance in kilometers
    /// </summary>
    public double DistanceTo(Coordinates other)
    {
        const double earthRadiusKm = 6371;

        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    /// <summary>
    /// Check if coordinates are within Egypt's approximate boundaries
    /// Egypt: Latitude: 22° to 32° N, Longitude: 25° to 35° E
    /// </summary>
    public bool IsWithinEgypt()
    {
        return Latitude >= 22 && Latitude <= 32 &&
               Longitude >= 25 && Longitude <= 35;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Math.Round(Latitude, 6); // Round to ~1 meter precision
        yield return Math.Round(Longitude, 6);
    }

    public override string ToString()
    {
        return $"{Latitude:F6}, {Longitude:F6}";
    }
}