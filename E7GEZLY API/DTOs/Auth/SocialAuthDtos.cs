namespace E7GEZLY_API.DTOs.Auth
{
    public record SocialLoginDto(
        string Provider,
        string AccessToken,
        string? DeviceName,
        string? DeviceType,
        string? UserAgent,
        string? IpAddress
    );

    public record SocialUserInfoDto(
        string Id,
        string? Email,
        string? Name,
        string? FirstName,
        string? LastName,
        string? Picture
    );

    public record AvailableProvidersDto(
        IEnumerable<string> Providers,
        bool IsAppleDevice
    );

    public record LinkedAccountDto(
        string Provider,
        string? Email,
        string? DisplayName,
        DateTime LinkedAt,
        DateTime? LastLoginAt
    );

    public record LinkedAccountsResponseDto(
        IEnumerable<LinkedAccountDto> LinkedAccounts
    );
}