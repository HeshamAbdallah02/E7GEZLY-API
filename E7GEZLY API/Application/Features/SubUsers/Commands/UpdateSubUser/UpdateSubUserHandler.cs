using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.UpdateSubUser
{
    /// <summary>
    /// Handler for UpdateSubUserCommand
    /// </summary>
    public class UpdateSubUserHandler : IRequestHandler<UpdateSubUserCommand, ApplicationResult<VenueSubUserResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IVenueSubUserService _venueSubUserService;
        private readonly IVenueAuditService _auditService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateSubUserHandler> _logger;

        public UpdateSubUserHandler(
            IApplicationDbContext context,
            IVenueSubUserService venueSubUserService,
            IVenueAuditService auditService,
            IDateTimeService dateTimeService,
            ICurrentUserService currentUserService,
            ILogger<UpdateSubUserHandler> logger)
        {
            _context = context;
            _venueSubUserService = venueSubUserService;
            _auditService = auditService;
            _dateTimeService = dateTimeService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueSubUserResponseDto>> Handle(UpdateSubUserCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await ((AppDbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Get existing sub-user (EF model, not domain entity)
                var existingSubUser = await _context.VenueSubUsers
                    .FirstOrDefaultAsync(su => su.Id == request.Id && su.VenueId == request.VenueId, 
                                       cancellationToken);

                if (existingSubUser == null)
                {
                    return ApplicationResult<VenueSubUserResponseDto>.Failure("Sub-user not found");
                }

                // Check for duplicate username if username is being changed
                if (!string.IsNullOrEmpty(request.Username) && 
                    !request.Username.Equals(existingSubUser.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var usernameExists = await _context.VenueSubUsers
                        .AnyAsync(su => su.Username == request.Username && 
                                       su.VenueId == request.VenueId && 
                                       su.Id != request.Id, 
                                cancellationToken);

                    if (usernameExists)
                    {
                        return ApplicationResult<VenueSubUserResponseDto>.Failure("Username already exists for another sub-user in this venue");
                    }
                }

                // TODO: Implement role and permissions validation
                // Basic validation - ensure required permissions are provided when role is specified
                if (request.Role.HasValue && !request.Permissions.HasValue)
                {
                    return ApplicationResult<VenueSubUserResponseDto>.Failure("Permissions must be specified when updating role");
                }

                // Store old values for audit
                var oldValues = new
                {
                    existingSubUser.Username,
                    existingSubUser.Role,
                    existingSubUser.Permissions,
                    existingSubUser.IsActive
                };

                // TODO: This handler violates Clean Architecture by directly accessing Domain entities through DbContext
                // Need to refactor to use proper Repository pattern and Domain services
                
                // Skip role and permission updates for now - requires proper Domain entity methods
                // if (request.Role.HasValue) { ... }
                // if (request.Permissions.HasValue) { ... }
                if (request.IsActive)
                {
                    existingSubUser.Activate();
                }
                else
                {
                    existingSubUser.Deactivate();
                }

                // If sub-user is being deactivated, terminate all active sessions
                int terminatedSessions = 0;
                if (!request.IsActive && oldValues.IsActive)
                {
                    var activeSessions = await _context.VenueSubUserSessions
                        .Where(s => s.SubUserId == request.Id && s.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var session in activeSessions)
                    {
                        session.Logout("Sub-user deactivated");
                        terminatedSessions++;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Create audit log
                var currentUserId = _currentUserService.UserId;
                await _auditService.LogVenueActionAsync(
                    request.VenueId,
                    currentUserId ?? "System",
                    "SubUserUpdated", // Convert enum to string
                    $"Sub-user '{existingSubUser.Username}' updated",
                    new
                    {
                        SubUserId = existingSubUser.Id,
                        OldValues = oldValues,
                        NewValues = new
                        {
                            existingSubUser.Username,
                            existingSubUser.Role,
                            existingSubUser.Permissions,
                            existingSubUser.IsActive
                        }
                    }
                );

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"Sub-user {existingSubUser.Id} updated for venue {request.VenueId}");

                // Prepare response
                var response = new VenueSubUserResponseDto(
                    existingSubUser.Id,
                    existingSubUser.Username,
                    existingSubUser.Role,
                    existingSubUser.Permissions,
                    existingSubUser.IsActive,
                    existingSubUser.IsFounderAdmin,
                    existingSubUser.CreatedAt,
                    existingSubUser.LastLoginAt,
                    null // CreatedByUsername - would need to fetch from repository
                );

                return ApplicationResult<VenueSubUserResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating sub-user {request.Id} for venue {request.VenueId}");
                await transaction.RollbackAsync(cancellationToken);
                return ApplicationResult<VenueSubUserResponseDto>.Failure("An error occurred while updating the sub-user");
            }
        }
    }
}