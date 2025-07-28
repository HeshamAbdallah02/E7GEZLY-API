// DTOs/User/UserDtos.cs
namespace E7GEZLY_API.DTOs.User
{
    public record UserInfoDto(
        string Id,
        string Email,
        string PhoneNumber,
        bool IsPhoneVerified,
        bool IsEmailVerified,
        bool IsActive,
        DateTime CreatedAt
    );

    public record UserSummaryDto(
        string Id,
        string Email,
        string PhoneNumber
    );
}