using E7GEZLY_API.DTOs.Customer;
using E7GEZLY_API.DTOs.Venue;

namespace E7GEZLY_API.Services.Auth
{
    public interface IProfileService
    {
        Task<CustomerProfileResponseDto?> GetCustomerProfileAsync(string userId);
        Task<VenueProfileResponseDto?> GetVenueProfileAsync(string userId);
        Task<bool> DeactivateAccountAsync(string userId, string password, string? reason);
    }
}