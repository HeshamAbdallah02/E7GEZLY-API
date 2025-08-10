using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.ValidateToken
{
    /// <summary>
    /// Handler for ValidateTokenQuery
    /// </summary>
    public class ValidateTokenHandler : IRequestHandler<ValidateTokenQuery, ApplicationResult<TokenValidationResultDto>>
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<ValidateTokenHandler> _logger;

        public ValidateTokenHandler(
            ITokenService tokenService,
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            ITokenBlacklistService tokenBlacklistService,
            IDateTimeService dateTimeService,
            ILogger<ValidateTokenHandler> logger)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _context = context;
            _tokenBlacklistService = tokenBlacklistService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<TokenValidationResultDto>> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = new TokenValidationResultDto
                {
                    IsValid = false,
                    IsExpired = false,
                    Message = "Invalid token"
                };

                // Basic token validation using TokenService
                var principal = await _tokenService.GetClaimsPrincipalFromTokenAsync(request.Token);
                if (principal == null)
                {
                    return ApplicationResult<TokenValidationResultDto>.Success(result with 
                    { 
                        Message = "Token validation failed" 
                    });
                }

                // Extract claims
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var venueIdClaim = principal.FindFirst("VenueId")?.Value;
                var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

                // Parse expiration
                DateTime? expiresAt = null;
                if (long.TryParse(expClaim, out var exp))
                {
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
                    
                    // Check if token is expired
                    if (expiresAt < _dateTimeService.UtcNow)
                    {
                        return ApplicationResult<TokenValidationResultDto>.Success(result with
                        {
                            IsExpired = true,
                            ExpiresAt = expiresAt,
                            Message = "Token has expired"
                        });
                    }
                }

                // Check if token is blacklisted
                if (!string.IsNullOrEmpty(jti) && await _tokenBlacklistService.IsTokenBlacklistedAsync(jti))
                {
                    return ApplicationResult<TokenValidationResultDto>.Success(result with
                    {
                        Message = "Token has been revoked"
                    });
                }

                // Parse VenueId
                Guid? venueId = null;
                if (!string.IsNullOrEmpty(venueIdClaim) && Guid.TryParse(venueIdClaim, out var parsedVenueId))
                {
                    venueId = parsedVenueId;
                }

                var validResult = new TokenValidationResultDto
                {
                    IsValid = true,
                    IsExpired = false,
                    UserId = userId,
                    UserEmail = email,
                    VenueId = venueId,
                    Roles = roles,
                    ExpiresAt = expiresAt,
                    Jti = jti,
                    Message = "Token is valid"
                };

                // Include additional user details if requested
                if (request.IncludeUserDetails && !string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.Users
                        .Include(u => u.Venue)
                        .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                    if (user != null && !user.IsActive)
                    {
                        return ApplicationResult<TokenValidationResultDto>.Success(result with
                        {
                            Message = "User account is inactive"
                        });
                    }

                    // Verify the user has at least one active session
                    var hasActiveSession = await _context.UserSessions
                        .AnyAsync(s => s.UserId == userId && 
                                  s.IsActive &&
                                  s.RefreshTokenExpiry > _dateTimeService.UtcNow, 
                                cancellationToken);

                    if (!hasActiveSession)
                    {
                        return ApplicationResult<TokenValidationResultDto>.Success(result with
                        {
                            Message = "No active session found"
                        });
                    }
                }

                return ApplicationResult<TokenValidationResultDto>.Success(validResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return ApplicationResult<TokenValidationResultDto>.Failure("An error occurred during token validation");
            }
        }
    }
}