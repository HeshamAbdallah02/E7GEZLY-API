// Attributes/RequireCompleteProfileAttribute.cs
using E7GEZLY_API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Attributes
{
    /// <summary>
    /// Ensures that venue users have completed their profile before accessing protected resources
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireCompleteProfileAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Only check for authenticated venue users
            if (user.Identity?.IsAuthenticated == true && user.IsInRole("VenueAdmin"))
            {
                var venueIdClaim = user.FindFirst("venueId")?.Value;
                if (!string.IsNullOrEmpty(venueIdClaim) && Guid.TryParse(venueIdClaim, out var venueId))
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                    var venue = await dbContext.Venues
                        .AsNoTracking()
                        .Select(v => new { v.Id, v.IsProfileComplete, v.VenueType })
                        .FirstOrDefaultAsync(v => v.Id == venueId);

                    if (venue != null && !venue.IsProfileComplete)
                    {
                        context.Result = new ObjectResult(new
                        {
                            error = "PROFILE_INCOMPLETE",
                            errorCode = "E7GEZLY_PROFILE_001",
                            message = "Please complete your venue profile to access this feature",
                            requiredAction = "COMPLETE_PROFILE",
                            metadata = new
                            {
                                venueId = venue.Id,
                                venueType = venue.VenueType.ToString(),
                                profileCompletionUrl = GetProfileCompletionUrl(venue.VenueType)
                            }
                        })
                        {
                            StatusCode = 403
                        };
                    }
                }
            }
        }

        private static string GetProfileCompletionUrl(Models.VenueType venueType)
        {
            return venueType switch
            {
                Models.VenueType.PlayStationVenue => "/api/venue/profile/complete/playstation",
                Models.VenueType.FootballCourt => "/api/venue/profile/complete/court",
                Models.VenueType.PadelCourt => "/api/venue/profile/complete/court",
                _ => "/api/venue/profile/complete"
            };
        }
    }
}