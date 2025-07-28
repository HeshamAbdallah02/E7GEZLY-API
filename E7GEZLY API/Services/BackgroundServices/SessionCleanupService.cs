// Services/BackgroundServices/SessionCleanupService.cs
using E7GEZLY_API.Services.Auth;

namespace E7GEZLY_API.Services.BackgroundServices
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupService> _logger;

        public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

                    await tokenService.CleanupExpiredSessionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session cleanup");
                }

                // Run cleanup every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
