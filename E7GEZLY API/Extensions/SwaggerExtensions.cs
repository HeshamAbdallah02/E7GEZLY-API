using Microsoft.OpenApi.Models;
using System.Reflection;

namespace E7GEZLY_API.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "E7GEZLY API",
                    Version = "v1",
                    Description = "Comprehensive venue booking platform for Egypt"
                });

                // Dynamic grouping based on namespace/folder structure
                options.TagActionsBy(api =>
                {
                    var controllerActionDescriptor = api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (controllerActionDescriptor != null)
                    {
                        var controllerType = controllerActionDescriptor.ControllerTypeInfo;
                        var namespaceParts = controllerType.Namespace?.Split('.');

                        // Check if controller is in a subfolder under Controllers
                        if (namespaceParts?.Length > 0 && namespaceParts.Contains("Controllers"))
                        {
                            var controllersIndex = Array.IndexOf(namespaceParts, "Controllers");
                            if (controllersIndex < namespaceParts.Length - 1)
                            {
                                // Controller is in a subfolder (e.g., Controllers.Auth)
                                var folder = namespaceParts[controllersIndex + 1];

                                // Map folder names to friendly names
                                var groupName = folder switch
                                {
                                    "Auth" => "Authentication",
                                    "Venues" => "Venue Management",
                                    "Bookings" => "Booking Management",
                                    "Payments" => "Payment Processing",
                                    "Users" => "User Management",
                                    _ => folder // Use folder name as-is
                                };

                                return new[] { groupName };
                            }
                        }

                        // Controller is directly in Controllers folder
                        // Use controller name without "Controller" suffix
                        var controllerName = controllerActionDescriptor.ControllerName;
                        return new[] { controllerName };
                    }

                    return new[] { "General" };
                });

                // Order tags alphabetically but with some priority
                options.OrderActionsBy((apiDesc) => $"{apiDesc.RelativePath}");

                // JWT Authentication configuration
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}