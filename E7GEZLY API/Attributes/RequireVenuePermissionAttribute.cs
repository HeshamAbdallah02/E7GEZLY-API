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

            // Debug logging for troubleshooting
            logger.LogDebug("Checking venue permissions. Required: {RequiredPermissions}", _requiredPermissions);

            // Check if user is authenticated
            if (user.Identity?.IsAuthenticated != true)
            {
                logger.LogWarning("Access denied: User not authenticated");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication required",
                    requiredPermission = _requiredPermissions.ToString()
                });
                return;
            }

            // Get token type
            var tokenType = user.FindFirst("type")?.Value;
            logger.LogDebug("Token type: {TokenType}", tokenType);

            // Ensure operational token
            if (!user.HasClaim("type", "venue-operational"))
            {
                logger.LogWarning("Access denied: Expected venue-operational token, got {TokenType}", tokenType);
                context.Result = new ObjectResult(new
                {
                    error = "INVALID_TOKEN_TYPE",
                    message = $"Venue operational token required. Current token type: {tokenType}",
                    expectedTokenType = "venue-operational",
                    actualTokenType = tokenType
                })
                {
                    StatusCode = 403
                };
                return;
            }

            // Get subUserId from claims
            var subUserIdClaim = user.FindFirst("subUserId")?.Value;
            if (!Guid.TryParse(subUserIdClaim, out var subUserId))
            {
                logger.LogWarning("Access denied: Invalid or missing subUserId in token");
                context.Result = new ObjectResult(new
                {
                    error = "INVALID_SUB_USER_CONTEXT",
                    message = "Invalid or missing sub-user ID in token",
                    subUserIdClaim = subUserIdClaim
                })
                {
                    StatusCode = 403
                };
                return;
            }

            // Get permissions from claims directly (faster than database lookup)
            var permissionsClaim = user.FindFirst("permissions")?.Value;
            if (!long.TryParse(permissionsClaim, out var userPermissionsValue))
            {
                logger.LogWarning("Access denied: Invalid or missing permissions in token for subUser {SubUserId}", subUserId);
                context.Result = new ObjectResult(new
                {
                    error = "INVALID_PERMISSIONS_CONTEXT",
                    message = "Invalid or missing permissions in token",
                    subUserId = subUserId,
                    permissionsClaim = permissionsClaim
                })
                {
                    StatusCode = 403
                };
                return;
            }

            var userPermissions = (VenuePermissions)userPermissionsValue;
            logger.LogDebug("User permissions: {UserPermissions} (value: {PermissionsValue})", userPermissions, userPermissionsValue);

            // Check if user has admin permissions (all permissions) - AdminPermissions = -1
            if (userPermissions == VenuePermissions.AdminPermissions)
            {
                logger.LogDebug("Permission check passed: User has AdminPermissions (bypass)");
                await next();
                return;
            }

            // Check if user has required permissions using bitwise AND
            var hasPermission = (userPermissions & _requiredPermissions) == _requiredPermissions;

            if (!hasPermission)
            {
                logger.LogWarning("Access denied: SubUser {SubUserId} lacks required permissions. " +
                    "Has: {UserPermissions} (value: {UserPermissionsValue}), " +
                    "Required: {RequiredPermissions} (value: {RequiredPermissionsValue})",
                    subUserId, userPermissions, userPermissionsValue,
                    _requiredPermissions, (long)_requiredPermissions);

                context.Result = new ObjectResult(new
                {
                    error = "INSUFFICIENT_PERMISSIONS",
                    errorCode = "E7GEZLY_PERM_001",
                    message = $"Access denied. Required permission: {_requiredPermissions}",
                    details = new
                    {
                        subUserId,
                        requiredPermissions = _requiredPermissions.ToString(),
                        requiredPermissionsValue = (long)_requiredPermissions,
                        userPermissions = userPermissions.ToString(),
                        userPermissionsValue = userPermissionsValue,
                        hasPermission = false,
                        isAdminUser = userPermissions == VenuePermissions.AdminPermissions,
                        bitwiseCheck = new
                        {
                            calculation = $"({userPermissionsValue} & {(long)_requiredPermissions}) == {(long)_requiredPermissions}",
                            result = $"{userPermissionsValue & (long)_requiredPermissions} == {(long)_requiredPermissions}",
                            explanation = "Bitwise AND operation to check if user has required permissions"
                        }
                    }
                })
                {
                    StatusCode = 403
                };
                return;
            }

            logger.LogDebug("Permission check passed for SubUser {SubUserId}. " +
                "Required: {RequiredPermissions}, User has: {UserPermissions}",
                subUserId, _requiredPermissions, userPermissions);

            await next();
        }
    }
}