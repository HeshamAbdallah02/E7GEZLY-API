using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.ResetSubUserPassword
{
    /// <summary>
    /// Handler for admin resetting sub-user password
    /// </summary>
    public class ResetSubUserPasswordHandler : IRequestHandler<ResetSubUserPasswordCommand, ApplicationResult<bool>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly ILogger<ResetSubUserPasswordHandler> _logger;

        public ResetSubUserPasswordHandler(
            IVenueSubUserService subUserService,
            ILogger<ResetSubUserPasswordHandler> logger)
        {
            _subUserService = subUserService;
            _logger = logger;
        }

        public async Task<ApplicationResult<bool>> Handle(ResetSubUserPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var resetPasswordDto = new ResetSubUserPasswordDto(request.NewPassword, request.MustChangePassword);
                await _subUserService.ResetPasswordAsync(request.VenueId, request.SubUserId, 
                    request.ResetBySubUserId, resetPasswordDto);
                
                return ApplicationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for sub-user {SubUserId} in venue {VenueId}", 
                    request.SubUserId, request.VenueId);
                return ApplicationResult<bool>.Failure("An error occurred while resetting the password");
            }
        }
    }
}