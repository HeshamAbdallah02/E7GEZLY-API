// DTOs/Auth/AuthResponseDto.cs
namespace E7GEZLY_API.DTOs.Auth
{
    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        string UserType,
        Dictionary<string, string> UserInfo
    );
}