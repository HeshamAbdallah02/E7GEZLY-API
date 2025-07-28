// DTOs/Auth/SessionDtos.cs
namespace E7GEZLY_API.DTOs.Auth
{
    public record UserSessionDto(
        Guid Id,
        string DeviceName,
        string DeviceType,
        string? IpAddress,
        string? City,
        string? Country,
        DateTime LastActivityAt,
        bool IsCurrent
    );

    public record CreateSessionDto(
        string? DeviceName,
        string? DeviceType,
        string? UserAgent,
        string? IpAddress
    );

    public record SessionsResponseDto(
        IEnumerable<UserSessionDto> Sessions,
        int ActiveSessionsCount
    );
}