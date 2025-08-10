using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.ResetSubUserPassword
{
    /// <summary>
    /// Command for admin resetting sub-user password
    /// </summary>
    public class ResetSubUserPasswordCommand : IRequest<ApplicationResult<bool>>
    {
        public Guid VenueId { get; init; }
        public Guid SubUserId { get; init; }
        public Guid ResetBySubUserId { get; init; }
        public string NewPassword { get; init; } = string.Empty;
        public bool MustChangePassword { get; init; } = true;
    }
}