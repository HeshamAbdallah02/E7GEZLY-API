using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.LogoutSubUser
{
    /// <summary>
    /// Handler for sub-user logout
    /// </summary>
    public class LogoutSubUserHandler : IRequestHandler<LogoutSubUserCommand, ApplicationResult<bool>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly ILogger<LogoutSubUserHandler> _logger;

        public LogoutSubUserHandler(
            IVenueSubUserService subUserService,
            ILogger<LogoutSubUserHandler> logger)
        {
            _subUserService = subUserService;
            _logger = logger;
        }

        public async Task<ApplicationResult<bool>> Handle(LogoutSubUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _subUserService.LogoutAsync(request.SubUserId);
                return ApplicationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sub-user logout {SubUserId}", request.SubUserId);
                return ApplicationResult<bool>.Failure("An error occurred during logout");
            }
        }
    }
}