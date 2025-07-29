using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 32 characters long");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                ConfigureJwtBearerOptions(opts, configuration, environment, jwtKey);
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("CustomerOnly", policy =>
                    policy.RequireRole("Customer"));

                options.AddPolicy("VenueOnly", policy =>
                    policy.RequireRole("VenueAdmin"));

                options.AddPolicy("DesktopAccess", policy =>
                   policy.RequireRole("VenueAdmin")
                         .RequireAssertion(context =>
                         {
                             // Check if the venue type is PlayStation
                             var venueTypeClaim = context.User.FindFirst("venueType")?.Value;
                             return venueTypeClaim == VenueType.PlayStationVenue.ToString() ||
                                    venueTypeClaim == ((int)VenueType.PlayStationVenue).ToString();
                         }));
            });

            return services;
        }

        private static void ConfigureJwtBearerOptions(JwtBearerOptions opts,
            IConfiguration configuration, IWebHostEnvironment environment, string jwtKey)
        {
            opts.RequireHttpsMetadata = !environment.IsDevelopment();
            opts.SaveToken = true;

            var validIssuers = new List<string>();
            var validAudiences = new List<string>();

            if (environment.IsDevelopment())
            {
                SetupDevelopmentIssuersAndAudiences(configuration, validIssuers, validAudiences);
            }
            else
            {
                SetupProductionIssuersAndAudiences(configuration, validIssuers, validAudiences);
            }

            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuers = validIssuers,
                ValidAudiences = validAudiences,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(5),
                AudienceValidator = environment.IsDevelopment() ? DevelopmentAudienceValidator : null
            };

            if (environment.IsDevelopment())
            {
                SetupDevelopmentValidators(opts);
                SetupDevelopmentEvents(opts);
            }
        }

        private static void SetupDevelopmentIssuersAndAudiences(IConfiguration configuration,
            List<string> validIssuers, List<string> validAudiences)
        {
            var configuredIssuers = configuration.GetSection("Jwt:ValidIssuers").Get<string[]>();
            if (configuredIssuers?.Any() == true)
            {
                validIssuers.AddRange(configuredIssuers);
            }
            else
            {
                validIssuers.AddRange(new[]
                {
                    "https://localhost:7092",
                    "https://localhost:5129",
                    "http://localhost:7092",
                    "http://localhost:5129",
                    configuration["Jwt:Issuer"] ?? "https://localhost:5001"
                });
            }

            var configuredAudiences = configuration.GetSection("Jwt:ValidAudiences").Get<string[]>();
            if (configuredAudiences?.Any() == true)
            {
                validAudiences.AddRange(configuredAudiences);
            }
            else
            {
                validAudiences.AddRange(validIssuers);
            }
        }

        private static void SetupProductionIssuersAndAudiences(IConfiguration configuration,
            List<string> validIssuers, List<string> validAudiences)
        {
            var issuer = configuration["Jwt:Issuer"] ??
                throw new InvalidOperationException("JWT Issuer not configured");
            validIssuers.Add(issuer);
            validAudiences.Add(configuration["Jwt:Audience"] ?? issuer);
        }

        private static bool DevelopmentAudienceValidator(IEnumerable<string> audiences,
            SecurityToken token, TokenValidationParameters parameters)
        {
            return audiences != null && audiences.Any(audience =>
                !string.IsNullOrEmpty(audience) &&
                (audience.Contains("localhost") ||
                 audience.Contains("ngrok.io") ||
                 audience.Contains("ngrok-free.app")));
        }

        private static void SetupDevelopmentValidators(JwtBearerOptions opts)
        {
            var originalIssuerValidator = opts.TokenValidationParameters.IssuerValidator;
            opts.TokenValidationParameters.IssuerValidator = (issuer, token, parameters) =>
            {
                if (!string.IsNullOrEmpty(issuer) &&
                    (issuer.Contains("localhost") ||
                     issuer.Contains("ngrok.io") ||
                     issuer.Contains("ngrok-free.app")))
                {
                    return issuer;
                }

                return originalIssuerValidator?.Invoke(issuer, token, parameters) ?? issuer;
            };
        }

        private static void SetupDevelopmentEvents(JwtBearerOptions opts)
        {
            opts.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Token validated for user: {UserId}",
                        context.Principal?.FindFirst("sub")?.Value);
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError("Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Challenge: {Error}", context.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        }
    }
}