using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.LinkSocialAccount
{
    /// <summary>
    /// Handler for linking social account to existing user account
    /// </summary>
    public class LinkSocialAccountHandler : IRequestHandler<LinkSocialAccountCommand, OperationResult<string>>
    {
        private readonly ISocialAuthService _socialAuthService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly ILogger<LinkSocialAccountHandler> _logger;

        public LinkSocialAccountHandler(
            ISocialAuthService socialAuthService,
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            ILogger<LinkSocialAccountHandler> logger)
        {
            _socialAuthService = socialAuthService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(LinkSocialAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return OperationResult<string>.Failure("User not found");
                }

                // Validate token
                var providerUser = await _socialAuthService.ValidateProviderTokenAsync(request.Provider, request.AccessToken);
                if (providerUser == null)
                {
                    return OperationResult<string>.Failure("Invalid social media token");
                }

                // Check if this social account is already linked to another user
                var existingLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(e => e.Provider == request.Provider && e.ProviderUserId == providerUser.Id, cancellationToken);

                if (existingLogin != null)
                {
                    if (existingLogin.UserId == request.UserId)
                    {
                        return OperationResult<string>.Failure("This social account is already linked to your profile");
                    }
                    return OperationResult<string>.Failure("This social account is already linked to another user");
                }

                // Create external login
                var externalLogin = Domain.Entities.ExternalLogin.Create(
                    request.UserId,
                    request.Provider,
                    providerUser.Id,
                    providerUser.Email,
                    providerUser.Name
                );

                _context.ExternalLogins.Add(externalLogin);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Linked {Provider} account to user {UserId}", request.Provider, request.UserId);

                return OperationResult<string>.Success($"{request.Provider} account linked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking social account for user {UserId}", request.UserId);
                return OperationResult<string>.Failure("An error occurred while linking social account");
            }
        }
    }
}