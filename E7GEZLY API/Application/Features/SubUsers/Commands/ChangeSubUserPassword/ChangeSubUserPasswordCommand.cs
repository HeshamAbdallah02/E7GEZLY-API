using E7GEZLY_API.Application.Common.Interfaces;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.ChangeSubUserPassword
{
    /// <summary>
    /// Command for changing sub-user password
    /// </summary>
    public class ChangeSubUserPasswordCommand : IRequest<OperationResult<bool>>
    {
        public Guid VenueId { get; init; }
        public Guid SubUserId { get; init; }
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}