// DTOs/Auth/AuthResponseDto.cs
namespace E7GEZLY_API.DTOs.Auth
{
    /// <summary>
    /// Basic auth response with tokens
    /// </summary>
    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        string UserType,
        Dictionary<string, string> UserInfo
    );

    /// <summary>
    /// Extended auth response with required actions
    /// </summary>
    public record AuthResponseWithActionsDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        string UserType,
        Dictionary<string, string> UserInfo,
        List<string> RequiredActions,
        AuthMetadataDto? Metadata
    ) : AuthResponseDto(AccessToken, RefreshToken, AccessTokenExpiry, UserType, UserInfo);

    /// <summary>
    /// Metadata for guiding frontend actions
    /// </summary>
    public record AuthMetadataDto(
        string? ProfileCompletionUrl,
        string? NextStepDescription,
        Dictionary<string, object>? AdditionalData
    );

    /// <summary>
    /// Venue-specific auth info
    /// </summary>
    public record VenueAuthInfoDto(
        Guid Id,
        string Name,
        string Type,
        bool IsProfileComplete,
        object? Location
    );

    /// <summary>
    /// User info in auth responses
    /// </summary>
    public record UserAuthInfoDto(
        string Id,
        string Email,
        string? PhoneNumber,
        bool IsPhoneVerified,
        bool IsEmailVerified
    );
}