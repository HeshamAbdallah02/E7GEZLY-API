// E7GEZLY API/Services/VenueManagement/VenueSubUserService.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Exceptions;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Cache;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SendGrid.Helpers.Errors.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E7GEZLY_API.Services.VenueManagement
{
    /// <summary>
    /// Implementation of venue sub-user management service
    /// </summary>
    public class VenueSubUserService : IVenueSubUserService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<VenueSubUser> _passwordHasher;
        private readonly IVenueAuditService _auditService;
        private readonly ICacheService _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VenueSubUserService> _logger;

        public VenueSubUserService(
            AppDbContext context,
            IPasswordHasher<VenueSubUser> passwordHasher,
            IVenueAuditService auditService,
            ICacheService cache,
            IConfiguration configuration,
            ILogger<VenueSubUserService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<VenueSubUserLoginResponseDto> AuthenticateSubUserAsync(
            Guid venueId,
            VenueSubUserLoginDto dto)
        {
            var subUser = await _context.VenueSubUsers
                .FirstOrDefaultAsync(u =>
                    u.VenueId == venueId &&
                    u.Username.ToUpper() == dto.Username.ToUpper());

            if (subUser == null)
            {
                await _auditService.LogActionAsync(new CreateAuditLogDto(
                    venueId,
                    null,
                    VenueAuditActions.SubUserLoginFailed,
                    "VenueSubUser",
                    dto.Username,
                    AdditionalData: new { Reason = "UserNotFound" }
                ));

                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Check lockout
            if (subUser.LockoutEnd.HasValue && subUser.LockoutEnd > DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException(
                    $"Account locked until {subUser.LockoutEnd:yyyy-MM-dd HH:mm} UTC");
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(
                subUser,
                subUser.PasswordHash,
                dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                // Increment failed attempts
                subUser.FailedLoginAttempts++;

                // Lock account after 5 failed attempts
                if (subUser.FailedLoginAttempts >= 5)
                {
                    subUser.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(new CreateAuditLogDto(
                    venueId,
                    subUser.Id,
                    VenueAuditActions.SubUserLoginFailed,
                    "VenueSubUser",
                    subUser.Id.ToString(),
                    AdditionalData: new
                    {
                        Reason = "InvalidPassword",
                        FailedAttempts = subUser.FailedLoginAttempts
                    }
                ));

                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Check if active
            if (!subUser.IsActive)
            {
                throw new UnauthorizedAccessException("Account is deactivated");
            }

            // Reset failed attempts
            subUser.FailedLoginAttempts = 0;
            subUser.LockoutEnd = null;
            subUser.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate tokens using custom method for sub-users
            var accessToken = GenerateSubUserAccessToken(subUser, venueId);
            var refreshToken = GenerateRefreshToken();

            var session = new VenueSubUserSession
            {
                SubUserId = subUser.Id,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(30),
                DeviceName = "Sub-User Session",
                DeviceType = "Unknown",
                IsActive = true,
                LastActivityAt = DateTime.UtcNow
            };

            _context.VenueSubUserSessions.Add(session);
            await _context.SaveChangesAsync();

            // Log successful login
            await _auditService.LogActionAsync(new CreateAuditLogDto(
                venueId,
                subUser.Id,
                VenueAuditActions.SubUserLogin,
                "VenueSubUser",
                subUser.Id.ToString()
            ));

            // Cache permissions
            await _cache.SetAsync(
                $"venue:{venueId}:subuser:{subUser.Id}:permissions",
                subUser.Permissions,
                TimeSpan.FromMinutes(5));

            return new VenueSubUserLoginResponseDto(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddHours(4),
                MapToResponseDto(subUser),
                subUser.MustChangePassword
            );
        }

        public async Task<VenueSubUserResponseDto> CreateSubUserAsync(
            Guid venueId,
            Guid? createdBySubUserId,
            CreateVenueSubUserDto dto)
        {
            // Check if username exists
            var exists = await _context.VenueSubUsers
                .AnyAsync(u =>
                    u.VenueId == venueId &&
                    u.Username.ToUpper() == dto.Username.ToUpper());

            if (exists)
            {
                throw new InvalidOperationException("Username already exists in this venue");
            }

            var subUser = new VenueSubUser
            {
                VenueId = venueId,
                Username = dto.Username,
                Role = dto.Role,
                Permissions = dto.Permissions ?? GetDefaultPermissions(dto.Role),
                CreatedBySubUserId = createdBySubUserId,
                MustChangePassword = true
            };

            // Hash password
            subUser.PasswordHash = _passwordHasher.HashPassword(subUser, dto.Password);

            _context.VenueSubUsers.Add(subUser);
            await _context.SaveChangesAsync();

            // Log creation
            await _auditService.LogActionAsync(new CreateAuditLogDto(
                venueId,
                createdBySubUserId,
                VenueAuditActions.SubUserCreated,
                "VenueSubUser",
                subUser.Id.ToString(),
                NewValues: new
                {
                    Username = subUser.Username,
                    Role = subUser.Role.ToString(),
                    Permissions = subUser.Permissions.ToString()
                }
            ));

            return MapToResponseDto(subUser);
        }

        public async Task<VenueSubUserResponseDto> UpdateSubUserAsync(
            Guid venueId,
            Guid subUserId,
            UpdateVenueSubUserDto dto)
        {
            var subUser = await GetSubUserEntityAsync(venueId, subUserId);

            var oldValues = new
            {
                Role = subUser.Role.ToString(),
                Permissions = subUser.Permissions.ToString(),
                IsActive = subUser.IsActive
            };

            if (dto.Role.HasValue)
                subUser.Role = dto.Role.Value;

            if (dto.Permissions.HasValue)
                subUser.Permissions = dto.Permissions.Value;

            if (dto.IsActive.HasValue)
                subUser.IsActive = dto.IsActive.Value;

            subUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Clear cached permissions
            await _cache.RemoveAsync($"venue:{venueId}:subuser:{subUserId}:permissions");

            var newValues = new
            {
                Role = subUser.Role.ToString(),
                Permissions = subUser.Permissions.ToString(),
                IsActive = subUser.IsActive
            };

            // Log update
            await _auditService.LogActionAsync(new CreateAuditLogDto(
                venueId,
                subUserId,
                VenueAuditActions.SubUserUpdated,
                "VenueSubUser",
                subUser.Id.ToString(),
                OldValues: oldValues,
                NewValues: newValues
            ));

            return MapToResponseDto(subUser);
        }

        public async Task DeleteSubUserAsync(
            Guid venueId,
            Guid subUserId,
            Guid deletedBySubUserId)
        {
            var subUser = await GetSubUserEntityAsync(venueId, subUserId);

            if (subUser.IsFounderAdmin)
            {
                throw new InvalidOperationException("Cannot delete the founder admin");
            }

            _context.VenueSubUsers.Remove(subUser);
            await _context.SaveChangesAsync();

            // Clear cached permissions
            await _cache.RemoveAsync($"venue:{venueId}:subuser:{subUserId}:permissions");

            // Log deletion
            await _auditService.LogActionAsync(new CreateAuditLogDto(
                venueId,
                deletedBySubUserId,
                VenueAuditActions.SubUserDeleted,
                "VenueSubUser",
                subUser.Id.ToString(),
                OldValues: new
                {
                    Username = subUser.Username,
                    Role = subUser.Role.ToString()
                }
            ));
        }

        public async Task<VenueSubUserResponseDto> ChangePasswordAsync(
            Guid venueId,
            Guid subUserId,
            ChangeSubUserPasswordDto dto)
        {
            var subUser = await GetSubUserEntityAsync(venueId, subUserId);

            // Verify current password
            var result = _passwordHasher.VerifyHashedPassword(
                subUser,
                subUser.PasswordHash,
                dto.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Update password
            subUser.PasswordHash = _passwordHasher.HashPassword(subUser, dto.NewPassword);
            subUser.PasswordChangedAt = DateTime.UtcNow;
            subUser.MustChangePassword = false;
            subUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log password change
            await _auditService.LogActionAsync(new CreateAuditLogDto(
                venueId,
                subUserId,
                VenueAuditActions.SubUserPasswordChanged,
                "VenueSubUser",
                subUser.Id.ToString()
            ));

            return MapToResponseDto(subUser);
        }

        public async Task<VenueSubUserResponseDto> ResetPasswordAsync(
            Guid venueId,
            Guid subUserId,
            Guid resetBySubUserId,
            ResetSubUserPasswordDto dto)
        {
            var subUser = await GetSubUserEntityAsync(venueId, subUserId);

            // Update password
            subUser.PasswordHash = _passwordHasher.HashPassword(subUser, dto.NewPassword);
            subUser.PasswordChangedAt = DateTime.UtcNow;
            subUser.MustChangePassword = dto.MustChangePassword;
            subUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log password reset
            await _auditService.LogActionAsync(new CreateAuditLogDto(
                venueId,
                resetBySubUserId,
                VenueAuditActions.SubUserPasswordReset,
                "VenueSubUser",
                subUser.Id.ToString(),
                AdditionalData: new { MustChangePassword = dto.MustChangePassword }
            ));

            return MapToResponseDto(subUser);
        }

        public async Task<IEnumerable<VenueSubUserResponseDto>> GetSubUsersAsync(Guid venueId)
        {
            var subUsers = await _context.VenueSubUsers
                .Include(u => u.CreatedBy)
                .Where(u => u.VenueId == venueId)
                .OrderBy(u => u.Username)
                .ToListAsync();

            return subUsers.Select(MapToResponseDto);
        }

        public async Task<VenueSubUserResponseDto?> GetSubUserAsync(
            Guid venueId,
            Guid subUserId)
        {
            var subUser = await _context.VenueSubUsers
                .Include(u => u.CreatedBy)
                .FirstOrDefaultAsync(u => u.VenueId == venueId && u.Id == subUserId);

            return subUser != null ? MapToResponseDto(subUser) : null;
        }

        public async Task<bool> ValidatePermissionsAsync(
            Guid subUserId,
            VenuePermissions requiredPermissions)
        {
            // Try cache first
            var cacheKey = $"venue:*:subuser:{subUserId}:permissions";
            var cachedPermissions = await _cache.GetAsync<VenuePermissions?>(cacheKey);

            if (cachedPermissions.HasValue)
            {
                return (cachedPermissions.Value & requiredPermissions) == requiredPermissions;
            }

            // Load from database
            var subUser = await _context.VenueSubUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == subUserId);

            if (subUser == null || !subUser.IsActive)
                return false;

            // Cache for next time
            await _cache.SetAsync(
                $"venue:{subUser.VenueId}:subuser:{subUserId}:permissions",
                subUser.Permissions,
                TimeSpan.FromMinutes(5));

            return (subUser.Permissions & requiredPermissions) == requiredPermissions;
        }

        public async Task RefreshTokenAsync(string refreshToken)
        {
            // Find and validate refresh token in VenueSubUserSessions instead of UserSessions
            var session = await _context.VenueSubUserSessions
                .Include(s => s.SubUser)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);

            if (session == null || session.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Update session activity
            session.LastActivityAt = DateTime.UtcNow;
            session.RefreshToken = GenerateRefreshToken(); // Generate new refresh token
            session.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync();
        }

        public async Task LogoutAsync(Guid subUserId)
        {
            // Clear cache
            await _cache.RemoveByPatternAsync($"venue:*:subuser:{subUserId}:*");

            // Deactivate venue sub-user sessions
            var sessions = await _context.VenueSubUserSessions
                .Where(s => s.SubUserId == subUserId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        #region Private Methods

        private async Task<VenueSubUser> GetSubUserEntityAsync(Guid venueId, Guid subUserId)
        {
            var subUser = await _context.VenueSubUsers
                .Include(u => u.CreatedBy)
                .FirstOrDefaultAsync(u => u.VenueId == venueId && u.Id == subUserId);

            if (subUser == null)
            {
                throw new E7GEZLY_API.Exceptions.NotFoundException("Sub-user not found");
            }

            return subUser;
        }

        private VenueSubUserResponseDto MapToResponseDto(VenueSubUser subUser)
        {
            return new VenueSubUserResponseDto(
                subUser.Id,
                subUser.Username,
                subUser.Role,
                subUser.Permissions,
                subUser.IsActive,
                subUser.IsFounderAdmin,
                subUser.CreatedAt,
                subUser.LastLoginAt,
                subUser.CreatedBy?.Username
            );
        }

        private VenuePermissions GetDefaultPermissions(VenueSubUserRole role)
        {
            return role switch
            {
                VenueSubUserRole.Admin => VenuePermissions.AdminPermissions,
                VenueSubUserRole.Coworker => VenuePermissions.CoworkerPermissions,
                _ => VenuePermissions.None
            };
        }

        private string GenerateSubUserAccessToken(VenueSubUser subUser, Guid venueId)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, subUser.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("venueId", venueId.ToString()),
                new Claim("subUserId", subUser.Id.ToString()),
                new Claim("subUserRole", subUser.Role.ToString()),
                new Claim("permissions", ((long)subUser.Permissions).ToString()),
                new Claim("type", "venue-operational")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(4);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        #endregion
    }
}