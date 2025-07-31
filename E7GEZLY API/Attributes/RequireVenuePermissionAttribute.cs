// E7GEZLY API/Attributes/RequireVenuePermissionAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using E7GEZLY_API.Models;
using System.Security.Claims;

namespace E7GEZLY_API.Attributes
{
    /// <summary>
    /// Ensures the sub-user has required permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireVenuePermissionAttribute : ActionFilterAttribute
    {
        private readonly VenuePermissions _requiredPermissions;

        public RequireVenuePermissionAttribute(VenuePermissions requiredPermissions)
        {
            _requiredPermissions = requiredPermissions;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<RequireVenuePermissionAttribute>>();

            // Ensure operational token
            if (!user.HasClaim("type", "venue-operational"))
            {
                logger.LogWarning("Access denied: Not a venue-operational token");
                context.Result = new ForbidResult("Venue operational access required");
                return;
            }

            // Get subUserId from claims
            var subUserIdClaim = user.FindFirst("subUserId")?.Value;
            if (!Guid.TryParse(subUserIdClaim, out var subUserId))
            {
                logger.LogWarning("Access denied: Invalid or missing subUserId in token");
                context.Result = new ForbidResult("Invalid sub-user context");
                return;
            }

            // Get permissions from claims directly (faster than database lookup)
            var permissionsClaim = user.FindFirst("permissions")?.Value;
            if (!long.TryParse(permissionsClaim, out var userPermissionsValue))
            {
                logger.LogWarning("Access denied: Invalid or missing permissions in token for subUser {SubUserId}", subUserId);
                context.Result = new ForbidResult("Invalid permissions context");
                return;
            }

            var userPermissions = (VenuePermissions)userPermissionsValue;

            // Check if user has required permissions using bitwise AND
            var hasPermission = (userPermissions & _requiredPermissions) == _requiredPermissions;

            if (!hasPermission)
            {
                logger.LogWarning("Access denied: SubUser {SubUserId} lacks required permissions. Has: {UserPermissions}, Required: {RequiredPermissions}",
                    subUserId, userPermissions, _requiredPermissions);

                context.Result = new ForbidResult(
                    $"Required permissions: {_requiredPermissions}. User has: {userPermissions}");
                return;
            }

            logger.LogDebug("Permission check passed for SubUser {SubUserId}. Required: {RequiredPermissions}, User has: {UserPermissions}",
                subUserId, _requiredPermissions, userPermissions);

            await next();
        }
    }
}