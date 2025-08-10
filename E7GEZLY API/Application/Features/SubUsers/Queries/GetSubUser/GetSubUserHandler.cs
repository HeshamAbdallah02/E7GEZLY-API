using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUser
{
    /// <summary>
    /// Handler for GetSubUserQuery
    /// </summary>
    public class GetSubUserHandler : IRequestHandler<GetSubUserQuery, ApplicationResult<VenueSubUserResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetSubUserHandler> _logger;

        public GetSubUserHandler(
            IApplicationDbContext context,
            ILogger<GetSubUserHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueSubUserResponseDto>> Handle(GetSubUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.VenueSubUsers
                    .Where(su => su.Id == request.Id && su.VenueId == request.VenueId);

                if (!request.IncludeInactive)
                {
                    query = query.Where(su => su.IsActive);
                }

                var subUser = await query.FirstOrDefaultAsync(cancellationToken);

                if (subUser == null)
                {
                    return ApplicationResult<VenueSubUserResponseDto>.Failure("Sub-user not found");
                }

                var response = new VenueSubUserResponseDto(
                    Id: subUser.Id,
                    Username: subUser.Username,
                    Role: subUser.Role,
                    Permissions: subUser.Permissions,
                    IsActive: subUser.IsActive,
                    IsFounderAdmin: subUser.IsFounderAdmin,
                    CreatedAt: subUser.CreatedAt,
                    LastLoginAt: subUser.LastLoginAt,
                    CreatedByUsername: null // This would need a join to get the creator's username
                );

                return ApplicationResult<VenueSubUserResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting sub-user {request.Id} for venue {request.VenueId}");
                return ApplicationResult<VenueSubUserResponseDto>.Failure("An error occurred while retrieving the sub-user");
            }
        }
    }
}