using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.User;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetUserProfile
{
    /// <summary>
    /// Query for getting user profile information
    /// </summary>
    public class GetUserProfileQuery : IRequest<ApplicationResult<UserProfileDto>>
    {
        public string UserId { get; init; } = string.Empty;
    }
}