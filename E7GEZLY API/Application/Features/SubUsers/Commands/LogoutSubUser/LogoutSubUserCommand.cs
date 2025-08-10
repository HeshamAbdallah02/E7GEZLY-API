using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.LogoutSubUser
{
    /// <summary>
    /// Command for sub-user logout
    /// </summary>
    public class LogoutSubUserCommand : IRequest<ApplicationResult<bool>>
    {
        public Guid SubUserId { get; init; }
    }
}