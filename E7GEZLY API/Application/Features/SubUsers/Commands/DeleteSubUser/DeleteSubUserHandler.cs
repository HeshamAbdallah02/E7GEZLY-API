using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.DeleteSubUser
{
    /// <summary>
    /// Handler for DeleteSubUserCommand
    /// </summary>
    public class DeleteSubUserHandler : IRequestHandler<DeleteSubUserCommand, ApplicationResult<DeleteSubUserResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IVenueAuditService _auditService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DeleteSubUserHandler> _logger;

        public DeleteSubUserHandler(
            IApplicationDbContext context,
            IVenueAuditService auditService,
            IDateTimeService dateTimeService,
            ICurrentUserService currentUserService,
            ILogger<DeleteSubUserHandler> logger)
        {
            _context = context;
            _auditService = auditService;
            _dateTimeService = dateTimeService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ApplicationResult<DeleteSubUserResponseDto>> Handle(DeleteSubUserCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await ((AppDbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Get the sub-user to delete
                var subUser = await _context.VenueSubUsers
                    .FirstOrDefaultAsync(su => su.Id == request.Id && su.VenueId == request.VenueId, 
                                       cancellationToken);

                if (subUser == null)
                {
                    return ApplicationResult<DeleteSubUserResponseDto>.Failure("Sub-user not found");
                }

                // Check for active sessions
                var activeSessions = await _context.VenueSubUserSessions
                    .Where(s => s.SubUserId == request.Id && s.IsActive)
                    .ToListAsync(cancellationToken);

                if (activeSessions.Any() && !request.ForceDelete)
                {
                    return ApplicationResult<DeleteSubUserResponseDto>.Failure(
                        $"Sub-user has {activeSessions.Count} active session(s). Use force delete to proceed.");
                }

                // Terminate all active sessions
                int terminatedSessions = 0;
                if (activeSessions.Any())
                {
                    foreach (var session in activeSessions)
                    {
                        session.Logout("Sub-user deleted");
                        terminatedSessions++;
                    }
                }

                // Store sub-user info for audit before deletion
                var deletedSubUserInfo = new
                {
                    subUser.Id,
                    Name = subUser.Username,
                    Email = (string?)null, // Domain VenueSubUser doesn't have email
                    PhoneNumber = (string?)null, // Domain VenueSubUser doesn't have phone
                    subUser.Role,
                    Permissions = subUser.Permissions.ToString(),
                    subUser.CreatedAt,
                    subUser.UpdatedAt,
                    subUser.LastLoginAt,
                    TerminatedSessions = terminatedSessions
                };

                // Delete the sub-user (this will cascade delete sessions due to FK)
                _context.VenueSubUsers.Remove(subUser);

                await _context.SaveChangesAsync(cancellationToken);

                // Create audit log
                var currentUserId = _currentUserService.UserId;
                await _auditService.LogVenueActionAsync(
                    request.VenueId,
                    currentUserId ?? "System",
                    "SubUserDeleted",
                    $"Sub-user '{subUser.Username}' deleted",
                    deletedSubUserInfo
                );

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"Sub-user {request.Id} deleted from venue {request.VenueId} with {terminatedSessions} terminated sessions");

                var response = new DeleteSubUserResponseDto
                {
                    Success = true,
                    Message = terminatedSessions > 0 ? 
                        $"Sub-user deleted successfully. {terminatedSessions} active session(s) were terminated." :
                        "Sub-user deleted successfully.",
                    DeletedSubUserId = request.Id,
                    TerminatedSessions = terminatedSessions
                };

                return ApplicationResult<DeleteSubUserResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting sub-user {request.Id} from venue {request.VenueId}");
                await transaction.RollbackAsync(cancellationToken);
                return ApplicationResult<DeleteSubUserResponseDto>.Failure("An error occurred while deleting the sub-user");
            }
        }
    }
}