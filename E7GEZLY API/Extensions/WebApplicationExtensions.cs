using E7GEZLY_API.Data;
using E7GEZLY_API.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace E7GEZLY_API.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                var dbContext = services.GetRequiredService<AppDbContext>();

                // Check if database is in-memory (used for testing)
                if (!dbContext.Database.IsInMemory())
                {
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrated successfully");
                }
                else
                {
                    // For in-memory database, just ensure it's created
                    await dbContext.Database.EnsureCreatedAsync();
                    logger.LogInformation("In-memory database created successfully");
                }

                await DbInitializer.SeedRolesAsync(services);
                logger.LogInformation("Roles seeded successfully");

                await LocationSeeder.SeedLocationsAsync(dbContext);
                logger.LogInformation("Locations seeded successfully");
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during startup initialization");

                // Re-throw in test environment to make tests fail fast
                if (app.Environment.EnvironmentName == "Test")
                {
                    throw;
                }
            }
        }

        public static void ConfigureMiddleware(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // Skip HTTPS redirection in test environment
            if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
            {
                app.UseHttpsRedirection();
            }

            // CORS
            if (app.Environment.IsDevelopment())
            {
                app.UseCors("DevelopmentCors");
                app.UseNgrokMiddleware();
            }
            else
            {
                app.UseCors("ProductionCors");
            }

            app.MapHealthChecks("/health");

            app.UseRateLimiting();

            app.UseAuthentication();

            app.UseTokenBlacklist();

            app.UseAuthorization();

            app.UseGlobalErrorHandler();

            app.MapControllers();
        }

        private static void UseNgrokMiddleware(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.ContainsKey("ngrok-skip-browser-warning"))
                {
                    context.Request.Headers.Remove("ngrok-skip-browser-warning");
                }

                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("Incoming request: {Method} {Path} from {Origin}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.Headers["Origin"].FirstOrDefault());

                await next();
            });
        }

        private static void UseGlobalErrorHandler(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Unhandled exception occurred");

                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";

                        var response = new
                        {
                            error = app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Test"
                                ? ex.Message
                                : "An error occurred processing your request",
                            timestamp = DateTime.UtcNow
                        };

                        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    }
                }
            });
        }
    }
}