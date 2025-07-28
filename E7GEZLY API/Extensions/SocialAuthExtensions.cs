using E7GEZLY_API.Services.Auth;

namespace E7GEZLY_API.Extensions
{
    public static class SocialAuthExtensions
    {
        public static IServiceCollection AddSocialAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Add HttpClient for external API calls to validate tokens
            services.AddHttpClient();

            // Register Social Auth Service
            services.AddScoped<ISocialAuthService, SocialAuthService>();

            // No OAuth middleware needed for direct token validation approach
            return services;
        }
    }
}