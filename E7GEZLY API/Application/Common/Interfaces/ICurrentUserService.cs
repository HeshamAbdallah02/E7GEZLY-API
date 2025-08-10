namespace E7GEZLY_API.Application.Common.Interfaces
{
    /// <summary>
    /// Service for accessing current user information in application layer
    /// </summary>
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        string? PhoneNumber { get; }
        bool IsAuthenticated { get; }
        bool IsVenueUser { get; }
        bool IsCustomer { get; }
        bool IsSubUser { get; }
        Guid? VenueId { get; }
        List<string> Roles { get; }
        List<string> Claims { get; }
    }
}