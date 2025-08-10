// Update your Program.cs - Add minimal cache service registration

using E7GEZLY_API.Application;
using E7GEZLY_API.Data;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Infrastructure;
using E7GEZLY_API.Middleware;
using E7GEZLY_API.Services.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// IMPORTANT: Register services in the correct order to avoid dependency issues

// 1. Core infrastructure services first
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);
builder.Services.AddApplication(); // Add Application layer
builder.Services.AddApplicationServices();
builder.Services.AddIdentityServices();

// 2. Add MINIMAL cache service registration (without decoration)
builder.Services.AddMinimalCacheServices(builder.Configuration);

// 3. Add venue sub-user services AFTER cache services
builder.Services.AddVenueSubUserServices();

// 4. Authentication and CORS
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCorsConfiguration(builder.Configuration);

// 5. Communication services
builder.Services.AddCommunicationServices(builder.Configuration, builder.Environment);
builder.Services.AddSocialAuthentication(builder.Configuration);

// 6. Controllers and Swagger
builder.Services.AddControllersWithOptions();
builder.Services.AddSwaggerConfiguration();

// 7. Health checks and background services
builder.Services.AddHealthCheckConfiguration();
builder.Services.AddHostedService<SessionCleanupService>();

// 8. Rate limiting
builder.Services.AddRateLimiting(builder.Configuration);

// 9. FULL distributed caching is disabled for now
// builder.Services.AddDistributedCaching(builder.Configuration);

var app = builder.Build();

// Initialize database and seed data
await app.InitializeDatabaseAsync();

// Use health checks
app.UseHealthChecks();

// Use Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline
app.ConfigureMiddleware();

// Cache warm-up is disabled for now
// app.UseDistributedCaching();

app.Run();

public partial class Program { }