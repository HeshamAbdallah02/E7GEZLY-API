// Services/Auth/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Domain.Entities;
using ApplicationUser = E7GEZLY_API.Models.ApplicationUser;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace E7GEZLY_API.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, CreateSessionDto? sessionInfo = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Add role claims
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var userInfo = new Dictionary<string, string>
            {
                ["userId"] = user.Id,
                ["email"] = user.Email!
            };

            // Add venue-specific claims
            if (user.VenueId.HasValue)
            {
                var venue = await _context.Venues.FindAsync(user.VenueId.Value);
                if (venue != null)
                {
                    claims.Add(new Claim("venueId", venue.Id.ToString()));
                    claims.Add(new Claim("venueName", venue.Name.Value));
                    claims.Add(new Claim("venueType", venue.VenueType.ToString()));

                    userInfo["venueId"] = venue.Id.ToString();
                    userInfo["venueName"] = venue.Name.Value;
                    userInfo["venueType"] = venue.VenueType.ToString();
                    userInfo["userType"] = "Venue";
                }
            }
            else
            {
                // Customer claims
                var profile = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    claims.Add(new Claim("customerId", profile.Id.ToString()));
                    claims.Add(new Claim("fullName", profile.Name.FullName));

                    userInfo["customerId"] = profile.Id.ToString();
                    userInfo["fullName"] = profile.Name.FullName;
                    userInfo["userType"] = "Customer";
                }
            }

            // Generate tokens
            var accessToken = GenerateAccessToken(claims);
            var refreshToken = GenerateRefreshToken();

            // Create session instead of updating user
            var session = UserSession.Create(
                user.Id,
                refreshToken,
                DateTime.UtcNow.AddDays(30),
                sessionInfo?.DeviceName ?? "Unknown Device",
                sessionInfo?.DeviceType ?? "Unknown",
                sessionInfo?.UserAgent,
                sessionInfo?.IpAddress
            );

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return new AuthResponseDto(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiry: DateTime.UtcNow.AddHours(4),
                UserType: user.VenueId.HasValue ? "Venue" : "Customer",
                UserInfo: userInfo
            );
        }

        private string GenerateAccessToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(4);

            // Use secure, fixed issuer and audience from configuration
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var audience = _configuration["Jwt:Audience"] ?? issuer;

            _logger.LogDebug("Generating token with secure issuer: {Issuer}", issuer);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // Implement RefreshTokensAsync with session support
        public async Task<AuthResponseDto?> RefreshTokensAsync(string refreshToken, string? ipAddress = null)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken &&
                                        s.RefreshTokenExpiry > DateTime.UtcNow &&
                                        s.IsActive);

            if (session == null)
                return null;

            // Get the user separately since domain entities don't have navigation properties
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == session.UserId);
            if (user == null || !user.IsActive)
            {
                // Deactivate session if user is inactive
                session.Deactivate();
                await _context.SaveChangesAsync();
                return null;
            }

            // Update session activity
            session.UpdateActivity();

            // Generate new tokens
            var newRefreshToken = GenerateRefreshToken();
            session.UpdateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(30));

            await _context.SaveChangesAsync();

            // Generate new access token with existing session info
            var sessionInfo = new CreateSessionDto(
                session.DeviceName,
                session.DeviceType,
                session.UserAgent,
                session.IpAddress
            );

            return await GenerateTokensAsync(user, sessionInfo);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ClaimsPrincipal?> GetClaimsPrincipalFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = false, // We'll handle lifetime validation separately
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        // Implement RevokeTokenAsync
        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

            if (session == null)
                return false;

            session.Deactivate();

            await _context.SaveChangesAsync();
            return true;
        }

        // Implement RevokeAllUserTokensAsync
        public async Task<bool> RevokeAllUserTokensAsync(string userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (!sessions.Any())
                return false;

            foreach (var session in sessions)
            {
                session.Deactivate();
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Implement RevokeSessionAsync
        public async Task<bool> RevokeSessionAsync(string userId, Guid sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
                return false;

            session.Deactivate();

            await _context.SaveChangesAsync();
            return true;
        }

        // Implement GetActiveSessionsAsync
        public async Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(string userId, string? currentRefreshToken = null)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .Select(s => new UserSessionDto(
                    s.Id,
                    s.DeviceName ?? "Unknown Device",
                    s.DeviceType ?? "Unknown",
                    s.IpAddress,
                    s.City,
                    s.Country,
                    s.LastActivityAt,
                    s.RefreshToken == currentRefreshToken
                ))
                .ToListAsync();

            return sessions;
        }

        // Implement CleanupExpiredSessionsAsync
        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.RefreshTokenExpiry < DateTime.UtcNow ||
                           (s.IsActive && s.LastActivityAt < DateTime.UtcNow.AddDays(-90)))
                .ToListAsync();

            if (expiredSessions.Any())
            {
                _context.UserSessions.RemoveRange(expiredSessions);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cleaned up {expiredSessions.Count} expired sessions");
            }
        }
    }
}