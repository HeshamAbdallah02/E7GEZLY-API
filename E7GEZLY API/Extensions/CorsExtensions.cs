namespace E7GEZLY_API.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                // Development CORS policy
                options.AddPolicy("DevelopmentCors", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "localhost" ||
                               uri.Host.EndsWith(".ngrok.io") ||
                               uri.Host.EndsWith(".ngrok-free.app");
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });

                // Production CORS policy
                options.AddPolicy("ProductionCors", policy =>
                {
                    policy.WithOrigins(
                            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                            new[] { "https://app.e7gezly.com", "https://www.e7gezly.com" }
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }
}