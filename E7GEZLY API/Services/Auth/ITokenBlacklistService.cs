// E7GEZLY API/Services/Auth/ITokenBlacklistService.cs
namespace E7GEZLY_API.Services.Auth
{
    /// <summary>
    /// Service for managing blacklisted JWT tokens
    /// </summary>
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// Blacklist a token by its JTI (JWT ID)
        /// </summary>
        /// <param name="tokenId">JWT ID (jti claim)</param>
        /// <param name="expirationTime">When the token naturally expires</param>
        /// <returns></returns>
        Task BlacklistTokenAsync(string tokenId, DateTime expirationTime);

        /// <summary>
        /// Check if a token is blacklisted
        /// </summary>
        /// <param name="tokenId">JWT ID (jti claim)</param>
        /// <returns>True if token is blacklisted</returns>
        Task<bool> IsTokenBlacklistedAsync(string tokenId);

        /// <summary>
        /// Blacklist all active tokens for a specific sub-user
        /// </summary>
        /// <param name="subUserId">Sub-user ID</param>
        /// <returns></returns>
        Task BlacklistAllSubUserTokensAsync(Guid subUserId);

        /// <summary>
        /// Clean up expired blacklisted tokens (called by background service)
        /// </summary>
        /// <returns></returns>
        Task CleanupExpiredTokensAsync();
    }
}