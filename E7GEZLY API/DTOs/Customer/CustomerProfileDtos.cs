// DTOs/Customer/CustomerProfileDtos.cs
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.User;

namespace E7GEZLY_API.DTOs.Customer
{
    public record CustomerProfileResponseDto(
        string UserType,
        UserInfoDto User,
        CustomerDetailsDto Profile
    );

    public record CustomerDetailsDto(
        Guid Id,
        string FirstName,
        string LastName,
        string FullName,
        DateTime? DateOfBirth,
        AddressResponseDto? Address
    );

    public record CustomerSummaryDto(
        Guid Id,
        string FullName,
        string? District,
        string? Governorate
    );
}