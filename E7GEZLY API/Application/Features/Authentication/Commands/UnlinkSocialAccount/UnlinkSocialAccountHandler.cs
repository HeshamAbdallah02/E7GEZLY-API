using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.UnlinkSocialAccount
{
    /// <summary>
    /// Handler for unlinking social account from user account
    /// </summary>
    public class UnlinkSocialAccountHandler : IRequestHandler<UnlinkSocialAccountCommand, OperationResult<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly ILogger<UnlinkSocialAccountHandler> _logger;

        public UnlinkSocialAccountHandler(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            ILogger<UnlinkSocialAccountHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UnlinkSocialAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return OperationResult<string>.Failure("User not found");
                }

                // Check if user has password (needed if unlinking last social account)
                var hasPassword = await _userManager.HasPasswordAsync(user);
                var socialLoginsCount = await _context.ExternalLogins
                    .CountAsync(e => e.UserId == request.UserId, cancellationToken);

                if (!hasPassword && socialLoginsCount <= 1)
                {
                    return OperationResult<string>.Failure("Cannot unlink the last social account without setting a password first");
                }

                var externalLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(e => e.UserId == request.UserId && e.Provider == request.Provider, cancellationToken);

                if (externalLogin == null)
                {
                    return OperationResult<string>.Failure($"{request.Provider} account is not linked");
                }

                _context.ExternalLogins.Remove(externalLogin);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Unlinked {Provider} account from user {UserId}", request.Provider, request.UserId);

                return OperationResult<string>.Success($"{request.Provider} account unlinked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking social account for user {UserId}", request.UserId);
                return OperationResult<string>.Failure("An error occurred while unlinking social account");
            }
        }
    }
}