using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUsers
{
    /// <summary>
    /// Handler for GetSubUsersQuery
    /// </summary>
    public class GetSubUsersHandler : IRequestHandler<GetSubUsersQuery, ApplicationResult<IEnumerable<VenueSubUserResponseDto>>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly ILogger<GetSubUsersHandler> _logger;

        public GetSubUsersHandler(
            IVenueSubUserService subUserService,
            ILogger<GetSubUsersHandler> logger)
        {
            _subUserService = subUserService;
            _logger = logger;
        }

        public async Task<ApplicationResult<IEnumerable<VenueSubUserResponseDto>>> Handle(GetSubUsersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var subUsers = await _subUserService.GetSubUsersAsync(request.VenueId);
                
                _logger.LogInformation($"Retrieved {subUsers.Count()} sub-users for venue {request.VenueId}");
                
                return ApplicationResult<IEnumerable<VenueSubUserResponseDto>>.Success(subUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sub-users for venue {request.VenueId}");
                return ApplicationResult<IEnumerable<VenueSubUserResponseDto>>.Failure(
                    "An error occurred while retrieving sub-users");
            }
        }
    }
}