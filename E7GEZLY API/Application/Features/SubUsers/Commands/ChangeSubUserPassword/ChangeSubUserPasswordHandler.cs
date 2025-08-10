using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.ChangeSubUserPassword
{
    /// <summary>
    /// Handler for changing sub-user password
    /// </summary>
    public class ChangeSubUserPasswordHandler : IRequestHandler<ChangeSubUserPasswordCommand, OperationResult<bool>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly ILogger<ChangeSubUserPasswordHandler> _logger;

        public ChangeSubUserPasswordHandler(
            IVenueSubUserService subUserService,
            ILogger<ChangeSubUserPasswordHandler> logger)
        {
            _subUserService = subUserService;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(ChangeSubUserPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var changePasswordDto = new ChangeSubUserPasswordDto(request.CurrentPassword, request.NewPassword);
                await _subUserService.ChangePasswordAsync(request.VenueId, request.SubUserId, changePasswordDto);
                
                return OperationResult<bool>.Success(true);
            }
            catch (UnauthorizedAccessException ex)
            {
                return OperationResult<bool>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for sub-user {SubUserId} in venue {VenueId}", 
                    request.SubUserId, request.VenueId);
                return OperationResult<bool>.Failure("An error occurred while changing the password");
            }
        }
    }
}