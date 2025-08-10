using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.DeleteSubUser
{
    /// <summary>
    /// Command for deleting a venue sub-user
    /// </summary>
    public class DeleteSubUserCommand : IRequest<ApplicationResult<DeleteSubUserResponseDto>>
    {
        public Guid Id { get; init; }
        public Guid VenueId { get; init; }
        public bool ForceDelete { get; init; } = false; // If true, delete even with active sessions
    }

    /// <summary>
    /// Response DTO for sub-user deletion
    /// </summary>
    public class DeleteSubUserResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public Guid DeletedSubUserId { get; init; }
        public int TerminatedSessions { get; init; }
    }
}