using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.CreateSubUser
{
    /// <summary>
    /// Handler for CreateSubUserCommand
    /// </summary>
    public class CreateSubUserHandler : IRequestHandler<CreateSubUserCommand, ApplicationResult<VenueSubUserResponseDto>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly ILogger<CreateSubUserHandler> _logger;

        public CreateSubUserHandler(
            IVenueSubUserService subUserService,
            ILogger<CreateSubUserHandler> logger)
        {
            _subUserService = subUserService;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueSubUserResponseDto>> Handle(CreateSubUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Map command to DTO
                var createDto = new CreateVenueSubUserDto(
                    Username: request.Username,
                    Password: request.Password,
                    Role: request.Role,
                    Permissions: request.Permissions
                );

                // Delegate to existing service
                var subUser = await _subUserService.CreateSubUserAsync(
                    request.VenueId,
                    request.CreatedBySubUserId,
                    createDto);

                _logger.LogInformation($"Sub-user created successfully: {subUser.Id} for venue {request.VenueId}");

                return ApplicationResult<VenueSubUserResponseDto>.Success(subUser);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid operation while creating sub-user for venue {request.VenueId}");
                return ApplicationResult<VenueSubUserResponseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating sub-user for venue {request.VenueId}");
                return ApplicationResult<VenueSubUserResponseDto>.Failure("An error occurred while creating the sub-user");
            }
        }
    }
}