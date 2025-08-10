// E7GEZLY API/Extensions/ClaimsPrincipalExtensions.cs
using System.Security.Claims;
using E7GEZLY_API.Models;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Extensions
{
    /// <summary>
    /// Extension methods for ClaimsPrincipal
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the venue ID from claims
        /// </summary>
        public static Guid GetVenueId(this ClaimsPrincipal user)
        {
            var venueIdClaim = user.FindFirst("venueId")?.Value;
            if (Guid.TryParse(venueIdClaim, out var venueId))
            {
                return venueId;
            }
            throw new UnauthorizedAccessException("Venue ID not found in claims");
        }

        /// <summary>
        /// Gets the sub-user ID from claims
        /// </summary>
        public static Guid GetSubUserId(this ClaimsPrincipal user)
        {
            var subUserIdClaim = user.FindFirst("subUserId")?.Value;
            if (Guid.TryParse(subUserIdClaim, out var subUserId))
            {
                return subUserId;
            }
            throw new UnauthorizedAccessException("Sub-user ID not found in claims");
        }

        /// <summary>
        /// Checks if the user belongs to the specified venue
        /// </summary>
        public static bool IsVenue(this ClaimsPrincipal user, Guid venueId)
        {
            try
            {
                return user.GetVenueId() == venueId;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the sub-user role from claims
        /// </summary>
        public static VenueSubUserRole GetSubUserRole(this ClaimsPrincipal user)
        {
            var roleClaim = user.FindFirst("subUserRole")?.Value;
            if (Enum.TryParse<VenueSubUserRole>(roleClaim, out var role))
            {
                return role;
            }
            throw new UnauthorizedAccessException("Sub-user role not found in claims");
        }

        /// <summary>
        /// Gets the venue permissions from claims
        /// </summary>
        public static VenuePermissions GetVenuePermissions(this ClaimsPrincipal user)
        {
            var permissionsClaim = user.FindFirst("permissions")?.Value;
            if (long.TryParse(permissionsClaim, out var permissions))
            {
                return (VenuePermissions)permissions;
            }
            throw new UnauthorizedAccessException("Venue permissions not found in claims");
        }

        /// <summary>
        /// Checks if user has venue gateway token
        /// </summary>
        public static bool IsVenueGateway(this ClaimsPrincipal user)
        {
            return user.HasClaim("type", "venue-gateway");
        }

        /// <summary>
        /// Checks if user has venue operational token
        /// </summary>
        public static bool IsVenueOperational(this ClaimsPrincipal user)
        {
            return user.HasClaim("type", "venue-operational");
        }

        /// <summary>
        /// Gets customer ID from claims (for existing functionality)
        /// </summary>
        public static Guid? GetCustomerId(this ClaimsPrincipal user)
        {
            var customerIdClaim = user.FindFirst("customerId")?.Value;
            if (Guid.TryParse(customerIdClaim, out var customerId))
            {
                return customerId;
            }
            return null;
        }
    }
}