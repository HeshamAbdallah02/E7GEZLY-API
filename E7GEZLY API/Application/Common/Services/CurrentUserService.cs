using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Extensions;
using System.Security.Claims;

namespace E7GEZLY_API.Application.Common.Services
{
    /// <summary>
    /// Implementation of ICurrentUserService using HttpContext
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        public string? PhoneNumber => _httpContextAccessor.HttpContext?.User?.FindFirstValue("phoneNumber");

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsVenueUser => _httpContextAccessor.HttpContext?.User?.IsInRole("VenueAdmin") ?? false;

        public bool IsCustomer => _httpContextAccessor.HttpContext?.User?.IsInRole("Customer") ?? false;

        public bool IsSubUser => _httpContextAccessor.HttpContext?.User?.HasClaim("type", "sub-user") ?? false;

        public Guid? VenueId
        {
            get
            {
                var venueIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("venueId");
                return Guid.TryParse(venueIdClaim, out var venueId) ? venueId : null;
            }
        }

        public List<string> Roles
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .FindAll(ClaimTypes.Role)?
                    .Select(c => c.Value)?
                    .ToList() ?? new List<string>();
            }
        }

        public List<string> Claims
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .Claims?
                    .Select(c => $"{c.Type}:{c.Value}")?
                    .ToList() ?? new List<string>();
            }
        }
    }
}