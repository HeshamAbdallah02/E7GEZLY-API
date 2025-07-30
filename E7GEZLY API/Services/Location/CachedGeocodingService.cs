using E7GEZLY_API.Configuration;
using E7GEZLY_API.Services.Caching;
using E7GEZLY_API.Services.Location;
using Microsoft.Extensions.Options;

namespace E7GEZLY_API.Services.Location
{
    public class CachedGeocodingService : IGeocodingService
    {
        private readonly IGeocodingService _innerService;
        private readonly ICacheService _cache;
        private readonly CacheConfiguration _config;
        private readonly ILogger<CachedGeocodingService> _logger;

        public CachedGeocodingService(
            IGeocodingService innerService,
            ICacheService cache,
            IOptions<CacheConfiguration> config,
            ILogger<CachedGeocodingService> logger)
        {
            _innerService = innerService;
            _cache = cache;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<GeocodingResult?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            if (!_config.Features.EnableGeocodingCache)
                return await _innerService.GetAddressFromCoordinatesAsync(latitude, longitude);

            var cacheKey = string.Format(CacheKeys.GeocodingResult, latitude.ToString("F6"), longitude.ToString("F6"));

            var cached = await _cache.GetAsync<GeocodingResult>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Geocoding cache hit for {Latitude}, {Longitude}", latitude, longitude);
                return cached;
            }

            var result = await _innerService.GetAddressFromCoordinatesAsync(latitude, longitude);

            if (result != null)
            {
                await _cache.SetAsync(
                    cacheKey,
                    result,
                    TimeSpan.FromDays(_config.Durations.GeocodingResultDays)
                );
                _logger.LogDebug("Cached geocoding result for {Latitude}, {Longitude}", latitude, longitude);
            }

            return result;
        }

        public async Task<int?> GetDistrictIdFromCoordinatesAsync(double latitude, double longitude)
        {
            var cacheKey = $"geocoding:district:{latitude:F6}:{longitude:F6}";

            if (_config.Features.EnableGeocodingCache)
            {
                var cached = await _cache.GetAsync<int?>(cacheKey);
                if (cached.HasValue)
                {
                    _logger.LogDebug("District cache hit for {Latitude}, {Longitude}", latitude, longitude);
                    return cached;
                }
            }

            var districtId = await _innerService.GetDistrictIdFromCoordinatesAsync(latitude, longitude);

            if (_config.Features.EnableGeocodingCache && districtId.HasValue)
            {
                await _cache.SetAsync(
                    cacheKey,
                    districtId,
                    TimeSpan.FromDays(_config.Durations.GeocodingResultDays)
                );
            }

            return districtId;
        }
    }
}