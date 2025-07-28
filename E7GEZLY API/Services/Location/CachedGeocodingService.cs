// Services/Location/CachedGeocodingService.cs
using E7GEZLY_API.Services.Location;
using Microsoft.Extensions.Caching.Memory;

public class CachedGeocodingService : IGeocodingService
{
    private readonly IGeocodingService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedGeocodingService> _logger;

    public CachedGeocodingService(
        IGeocodingService innerService,
        IMemoryCache cache,
        ILogger<CachedGeocodingService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        var cacheKey = $"geocode_{latitude:F6}_{longitude:F6}";

        if (_cache.TryGetValue<GeocodingResult>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug($"Geocoding cache hit for {latitude}, {longitude}");
            return cachedResult;
        }

        var result = await _innerService.GetAddressFromCoordinatesAsync(latitude, longitude);

        if (result != null)
        {
            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                SlidingExpiration = TimeSpan.FromHours(6)
            });
            _logger.LogDebug($"Cached geocoding result for {latitude}, {longitude}");
        }

        return result;
    }

    public async Task<int?> GetDistrictIdFromCoordinatesAsync(double latitude, double longitude)
    {
        var cacheKey = $"district_{latitude:F6}_{longitude:F6}";

        if (_cache.TryGetValue<int?>(cacheKey, out var cachedDistrictId))
        {
            _logger.LogDebug($"District cache hit for {latitude}, {longitude}");
            return cachedDistrictId;
        }

        var districtId = await _innerService.GetDistrictIdFromCoordinatesAsync(latitude, longitude);

        _cache.Set(cacheKey, districtId, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            SlidingExpiration = TimeSpan.FromHours(6)
        });

        return districtId;
    }
}