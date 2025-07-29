using E7GEZLY_API.Data;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Middleware;
using E7GEZLY_API.Services.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddControllersWithOptions();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddHealthCheckConfiguration();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddCommunicationServices(builder.Configuration, builder.Environment);
builder.Services.AddSocialAuthentication(builder.Configuration);

var app = builder.Build();

// Initialize database and seed data
await app.InitializeDatabaseAsync();

// Use health checks
app.UseHealthChecks();

// Use Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline
app.ConfigureMiddleware();

app.Run();