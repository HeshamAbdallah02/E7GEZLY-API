// Services/Auth/ITokenService.cs
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Services.Auth
{
    public interface ITokenService
    {
        // Updated to accept session info
        Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, CreateSessionDto? sessionInfo = null);

        // New method for refresh token with session tracking
        Task<AuthResponseDto?> RefreshTokensAsync(string refreshToken, string? ipAddress = null);

        // Token validation method
        Task<bool> ValidateTokenAsync(string token);
        
        // Token validation method that returns claims principal
        Task<System.Security.Claims.ClaimsPrincipal?> GetClaimsPrincipalFromTokenAsync(string token);

        // Keep existing method
        string GenerateRefreshToken();

        // New session management methods
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> RevokeAllUserTokensAsync(string userId);
        Task<bool> RevokeSessionAsync(string userId, Guid sessionId);
        Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(string userId, string? currentRefreshToken = null);
        Task CleanupExpiredSessionsAsync();
    }
}