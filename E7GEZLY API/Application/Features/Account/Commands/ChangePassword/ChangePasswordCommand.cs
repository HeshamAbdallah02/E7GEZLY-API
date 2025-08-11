using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.ChangePassword
{
    /// <summary>
    /// Command for changing user password with optional logout from all devices
    /// </summary>
    public class ChangePasswordCommand : IRequest<ApplicationResult<object>>
    {
        public string UserId { get; init; } = string.Empty;
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public bool LogoutAllDevices { get; init; } = false;
    }
}