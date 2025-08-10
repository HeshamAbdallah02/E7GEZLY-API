using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.LoginSubUser
{
    /// <summary>
    /// Handler for sub-user authentication command
    /// Temporarily delegates to existing service for rapid Clean Architecture completion
    /// </summary>
    public class LoginSubUserHandler : IRequestHandler<LoginSubUserCommand, OperationResult<VenueSubUserLoginResponseDto>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly ILogger<LoginSubUserHandler> _logger;

        public LoginSubUserHandler(
            IVenueSubUserService subUserService,
            ILogger<LoginSubUserHandler> logger)
        {
            _subUserService = subUserService;
            _logger = logger;
        }

        public async Task<OperationResult<VenueSubUserLoginResponseDto>> Handle(LoginSubUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var loginDto = new VenueSubUserLoginDto(request.Username, request.Password);
                var result = await _subUserService.AuthenticateSubUserAsync(request.VenueId, loginDto);
                
                return OperationResult<VenueSubUserLoginResponseDto>.Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return OperationResult<VenueSubUserLoginResponseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sub-user authentication for venue {VenueId}", request.VenueId);
                return OperationResult<VenueSubUserLoginResponseDto>.Failure("An error occurred during authentication");
            }
        }
    }
}