using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.ValueObjects;

/// <summary>
/// Value object representing a person's name
/// Ensures name validation and provides formatting capabilities
/// </summary>
public sealed class PersonName : ValueObject
{
    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    public static PersonName Create(string firstName, string lastName)
    {
        // Validate first name
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new BusinessRuleViolationException("First name is required");
        }

        if (firstName.Length < 2)
        {
            throw new BusinessRuleViolationException("First name must be at least 2 characters long");
        }

        if (firstName.Length > 50)
        {
            throw new BusinessRuleViolationException("First name cannot exceed 50 characters");
        }

        // Validate last name
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new BusinessRuleViolationException("Last name is required");
        }

        if (lastName.Length < 2)
        {
            throw new BusinessRuleViolationException("Last name must be at least 2 characters long");
        }

        if (lastName.Length > 50)
        {
            throw new BusinessRuleViolationException("Last name cannot exceed 50 characters");
        }

        // Clean and format names
        var cleanFirstName = CleanName(firstName);
        var cleanLastName = CleanName(lastName);

        return new PersonName(cleanFirstName, cleanLastName);
    }

    private static string CleanName(string name)
    {
        // Trim whitespace and standardize format
        var cleaned = name.Trim();
        
        // Convert to title case (first letter uppercase, rest lowercase)
        if (cleaned.Length > 0)
        {
            cleaned = char.ToUpperInvariant(cleaned[0]) + 
                     (cleaned.Length > 1 ? cleaned.Substring(1).ToLowerInvariant() : string.Empty);
        }

        return cleaned;
    }

    public PersonName WithFirstName(string firstName)
    {
        return Create(firstName, LastName);
    }

    public PersonName WithLastName(string lastName)
    {
        return Create(FirstName, lastName);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString()
    {
        return FullName;
    }
}