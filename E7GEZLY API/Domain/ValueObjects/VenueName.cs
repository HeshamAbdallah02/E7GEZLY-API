using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.ValueObjects;

/// <summary>
/// Value object representing a venue's name
/// Ensures name validation and provides formatting capabilities for venues
/// </summary>
public sealed class VenueName : ValueObject
{
    private VenueName(string name)
    {
        Name = name;
    }

    public string Name { get; }
    
    /// <summary>
    /// Alias for Name property - provides compatibility with application layer expectations
    /// </summary>
    public string Value => Name;

    public static VenueName Create(string name)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleViolationException("Venue name is required");
        }

        if (name.Length < 3)
        {
            throw new BusinessRuleViolationException("Venue name must be at least 3 characters long");
        }

        if (name.Length > 200)
        {
            throw new BusinessRuleViolationException("Venue name cannot exceed 200 characters");
        }

        // Clean the name
        var cleanName = CleanVenueName(name);

        return new VenueName(cleanName);
    }

    private static string CleanVenueName(string name)
    {
        // Trim whitespace and remove extra spaces
        var cleaned = name.Trim();
        
        // Replace multiple spaces with single space
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        return cleaned;
    }

    public bool ContainsKeyword(string keyword)
    {
        return Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    public VenueName WithName(string name)
    {
        return Create(name);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name.ToLowerInvariant();
    }

    public override string ToString()
    {
        return Name;
    }
}