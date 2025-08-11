using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.DeactivateAccount
{
    /// <summary>
    /// Handler for DeactivateAccountCommand using ProfileService logic
    /// </summary>
    public class DeactivateAccountHandler : IRequestHandler<DeactivateAccountCommand, ApplicationResult<object>>
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<DeactivateAccountHandler> _logger;

        public DeactivateAccountHandler(IProfileService profileService, ILogger<DeactivateAccountHandler> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _profileService.DeactivateAccountAsync(request.UserId, request.Password, request.Reason);
                
                if (!success)
                {
                    return ApplicationResult<object>.Failure("Failed to deactivate account. Please check your password.");
                }

                var response = new { message = "Account deactivated successfully" };
                return ApplicationResult<object>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating account for user {UserId}", request.UserId);
                return ApplicationResult<object>.Failure("An error occurred while deactivating account");
            }
        }
    }
}