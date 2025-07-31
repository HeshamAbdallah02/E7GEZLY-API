using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace E7GEZLY_API.Attributes
{
    /// <summary>
    /// Ensures the request has a valid venue gateway token
    /// </summary>
    public class RequireVenueGatewayAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            if (!user.HasClaim("type", "venue-gateway"))
            {
                context.Result = new ForbidResult("Venue gateway access required");
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}