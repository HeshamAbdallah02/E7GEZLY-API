using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.Events;
using E7GEZLY_API.Domain.ValueObjects;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity representing a customer's profile
/// Contains personal information and address details
/// </summary>
public sealed class CustomerProfile : AggregateRoot
{
    private CustomerProfile(string userId, PersonName name, DateTime? dateOfBirth = null, 
        Address? address = null, int? districtSystemId = null) : base()
    {
        UserId = userId;
        Name = name;
        DateOfBirth = dateOfBirth;
        Address = address ?? Address.CreateEmpty();
        DistrictSystemId = districtSystemId;
    }

    public static CustomerProfile Create(string userId, string firstName, string lastName, 
        DateTime? dateOfBirth = null, string? streetAddress = null, string? landmark = null,
        double? latitude = null, double? longitude = null, int? districtSystemId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessRuleViolationException("User ID is required for customer profile");

        var name = PersonName.Create(firstName, lastName);
        var address = Address.Create(streetAddress, landmark, latitude, longitude);

        // Validate date of birth if provided
        if (dateOfBirth.HasValue)
        {
            if (dateOfBirth.Value > DateTime.Today)
                throw new BusinessRuleViolationException("Date of birth cannot be in the future");

            if (dateOfBirth.Value < DateTime.Today.AddYears(-150))
                throw new BusinessRuleViolationException("Date of birth cannot be more than 150 years ago");
        }

        var profile = new CustomerProfile(userId, name, dateOfBirth, address, districtSystemId);
        
        profile.AddDomainEvent(new CustomerProfileUpdatedEvent(
            userId,
            firstName,
            lastName,
            DateTime.UtcNow));

        return profile;
    }

    public static CustomerProfile CreateExistingProfile(Guid id, string userId, string firstName, string lastName,
        DateTime? dateOfBirth, string? streetAddress, string? landmark, double? latitude, double? longitude,
        int? districtSystemId, DateTime createdAt, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessRuleViolationException("User ID is required for customer profile");

        var name = PersonName.Create(firstName, lastName);
        var address = Address.Create(streetAddress, landmark, latitude, longitude);

        var profile = new CustomerProfile(userId, name, dateOfBirth, address, districtSystemId);
        profile.SetId(id);
        profile.SetCreatedAt(createdAt);
        profile.SetUpdatedAt(updatedAt);

        return profile;
    }

    public string UserId { get; private set; }
    public PersonName Name { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public Address Address { get; private set; }
    public int? DistrictSystemId { get; private set; }

    // Navigation property - set by infrastructure layer
    public District? District { get; set; }

    public int? Age
    {
        get
        {
            if (!DateOfBirth.HasValue) return null;
            
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;
            
            // Adjust if birthday hasn't occurred this year yet
            if (DateOfBirth.Value.Date > today.AddYears(-age))
                age--;
                
            return age;
        }
    }

    public bool IsAddressComplete => !Address.IsEmpty && DistrictSystemId.HasValue;

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

    public void UpdateName(string firstName, string lastName)
    {
        var newName = PersonName.Create(firstName, lastName);
        
        if (Name == newName) return; // No change needed
        
        Name = newName;
        MarkAsUpdated();

        AddDomainEvent(new CustomerProfileUpdatedEvent(
            UserId,
            firstName,
            lastName,
            DateTime.UtcNow));
    }

    public void UpdateDateOfBirth(DateTime? dateOfBirth)
    {
        if (dateOfBirth.HasValue)
        {
            if (dateOfBirth.Value > DateTime.Today)
                throw new BusinessRuleViolationException("Date of birth cannot be in the future");

            if (dateOfBirth.Value < DateTime.Today.AddYears(-150))
                throw new BusinessRuleViolationException("Date of birth cannot be more than 150 years ago");
        }

        if (DateOfBirth == dateOfBirth) return; // No change needed

        DateOfBirth = dateOfBirth;
        MarkAsUpdated();
    }

    public void UpdateAddress(string? streetAddress, string? landmark, double? latitude = null, double? longitude = null)
    {
        var newAddress = Address.Create(streetAddress, landmark, latitude, longitude);
        
        if (Address == newAddress) return; // No change needed

        Address = newAddress;
        MarkAsUpdated();
    }

    public void UpdateDistrict(int? districtSystemId)
    {
        if (DistrictSystemId == districtSystemId) return; // No change needed

        DistrictSystemId = districtSystemId;
        MarkAsUpdated();
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        Address = Address.WithCoordinates(latitude, longitude);
        MarkAsUpdated();
    }

    public double? GetDistanceTo(Coordinates targetCoordinates)
    {
        return Address.Coordinates?.DistanceTo(targetCoordinates);
    }

    public bool IsInEgypt()
    {
        return Address.Coordinates?.IsWithinEgypt() ?? false;
    }

    public override string ToString()
    {
        return $"{Name.FullName} - {GetFullAddress()}";
    }
}