// File: Data/DbInitializer.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace E7GEZLY_API.Data
{
    public static class DbInitializer
    {
        public static class AppRoles
        {
            public const string Customer = "Customer";
            public const string VenueAdmin = "VenueAdmin";
            public const string SystemAdmin = "SystemAdmin";
        }

        public static async Task SeedRolesAsync(IServiceProvider svc)
        {
            var roleMgr = svc.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { AppRoles.Customer, AppRoles.VenueAdmin, AppRoles.SystemAdmin };

            foreach (var role in roles)
            {
                if (!await roleMgr.RoleExistsAsync(role))
                {
                    await roleMgr.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
