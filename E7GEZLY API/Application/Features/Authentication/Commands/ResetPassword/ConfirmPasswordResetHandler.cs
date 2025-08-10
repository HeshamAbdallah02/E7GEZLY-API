using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Handler for ConfirmPasswordResetCommand
    /// </summary>
    public class ConfirmPasswordResetHandler : IRequestHandler<ConfirmPasswordResetCommand, ApplicationResult<PasswordResetConfirmResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<ConfirmPasswordResetHandler> _logger;

        public ConfirmPasswordResetHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            IDateTimeService dateTimeService,
            ILogger<ConfirmPasswordResetHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<PasswordResetConfirmResponseDto>> Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await ((AppDbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Find user by email or phone
                ApplicationUser? user = null;

                if (request.EmailOrPhone.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
                }
                else
                {
                    var formattedPhone = request.EmailOrPhone.StartsWith("+2") ? 
                        request.EmailOrPhone : $"+2{request.EmailOrPhone}";
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone, cancellationToken);
                }

                if (user == null)
                {
                    return ApplicationResult<PasswordResetConfirmResponseDto>.Failure("Invalid reset information");
                }

                if (!user.IsActive)
                {
                    return ApplicationResult<PasswordResetConfirmResponseDto>.Failure("Account is inactive");
                }

                // Verify reset code
                if (string.IsNullOrEmpty(user.PasswordResetCode) || 
                    user.PasswordResetCode != request.ResetCode ||
                    user.PasswordResetCodeExpiry == null ||
                    user.PasswordResetCodeExpiry < _dateTimeService.UtcNow)
                {
                    return ApplicationResult<PasswordResetConfirmResponseDto>.Failure("Invalid or expired reset code");
                }

                // Reset password
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ApplicationResult<PasswordResetConfirmResponseDto>.Failure(
                        result.Errors.Select(e => e.Description).ToArray());
                }

                // Clear reset code
                user.PasswordResetCode = null;
                user.PasswordResetCodeExpiry = null;
                await _userManager.UpdateAsync(user);

                // Invalidate all user sessions (force re-login)
                var userSessions = await _context.UserSessions
                    .Where(s => s.UserId == user.Id && s.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var session in userSessions)
                {
                    session.Deactivate();
                }

                // Also invalidate sub-user sessions if this is a venue admin
                if (user.VenueId.HasValue)
                {
                    var subUserSessions = await _context.VenueSubUserSessions
                        .Where(s => s.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var session in subUserSessions)
                    {
                        session.Deactivate();
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"Password reset completed for user {user.Id}");

                var response = new PasswordResetConfirmResponseDto
                {
                    Success = true,
                    Message = "Password has been reset successfully. Please log in with your new password.",
                    RequiresLogin = true
                };

                return ApplicationResult<PasswordResetConfirmResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset confirmation");
                await transaction.RollbackAsync(cancellationToken);
                return ApplicationResult<PasswordResetConfirmResponseDto>.Failure("An error occurred while resetting the password");
            }
        }
    }
}