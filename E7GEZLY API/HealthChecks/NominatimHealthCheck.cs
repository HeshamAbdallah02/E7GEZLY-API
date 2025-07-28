// HealthChecks/NominatimHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace E7GEZLY_API.HealthChecks
{
    public class NominatimHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NominatimHealthCheck> _logger;

        public NominatimHealthCheck(HttpClient httpClient, ILogger<NominatimHealthCheck> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Test with Cairo coordinates
                var testUrl = "https://nominatim.openstreetmap.org/reverse?format=json&lat=30.0444&lon=31.2357&zoom=10";

                var response = await _httpClient.GetAsync(testUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Nominatim service is responsive");
                }

                return HealthCheckResult.Degraded($"Nominatim returned {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nominatim health check failed");
                return HealthCheckResult.Unhealthy("Nominatim service is not accessible", ex);
            }
        }
    }
}