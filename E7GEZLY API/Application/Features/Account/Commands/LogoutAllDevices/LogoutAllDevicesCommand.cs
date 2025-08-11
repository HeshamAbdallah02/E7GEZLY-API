using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.LogoutAllDevices
{
    /// <summary>
    /// Command for logging out from all devices/sessions
    /// </summary>
    public class LogoutAllDevicesCommand : IRequest<ApplicationResult<object>>
    {
        public string UserId { get; init; } = string.Empty;
    }
}