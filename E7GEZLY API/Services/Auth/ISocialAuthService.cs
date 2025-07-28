using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Services.Auth
{
    public interface ISocialAuthService
    {
        Task<SocialUserInfoDto?> ValidateProviderTokenAsync(string provider, string accessToken);
        Task<ApplicationUser?> FindOrCreateUserAsync(string provider, SocialUserInfoDto providerUser);
        Task UpdateExternalLoginAsync(ApplicationUser user, string provider, string providerUserId);
        IEnumerable<string> GetAvailableProviders(bool isAppleDevice);
    }
}