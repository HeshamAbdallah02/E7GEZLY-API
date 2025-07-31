using E7GEZLY_API.Configuration;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Cache;
using Microsoft.Extensions.Options;

namespace E7GEZLY_API.Services.Location
{
    /// <summary>
    /// Decorator for LocationService that adds caching
    /// </summary>
    public class CachedLocationService : ILocationService
    {
        private readonly ILocationService _locationService;
        private readonly ICacheService _cache;
        private readonly CacheConfiguration _cacheConfig;
        private readonly ILogger<CachedLocationService> _logger;

        public CachedLocationService(
            ILocationService locationService,
            ICacheService cache,
            IOptions<CacheConfiguration> cacheConfig,
            ILogger<CachedLocationService> logger)
        {
            _locationService = locationService;
            _cache = cache;
            _cacheConfig = cacheConfig.Value;
            _logger = logger;
        }

        public async Task<List<GovernorateDto>> GetGovernoratesAsync()
        {
            if (!_cacheConfig.Features.EnableLocationCache)
                return await _locationService.GetGovernoratesAsync();

            var cacheKey = CacheKeys.AllGovernorates;
            var cached = await _cache.GetAsync<List<GovernorateDto>>(cacheKey);

            if (cached != null)
            {
                _logger.LogDebug("Cache hit for all governorates");
                return cached;
            }

            var result = await _locationService.GetGovernoratesAsync();

            await _cache.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(_cacheConfig.Durations.LocationDataMinutes)
            );

            await _cache.TagAsync(cacheKey, CacheKeys.LocationTag);

            return result;
        }

        public async Task<List<DistrictDto>> GetDistrictsAsync(int? governorateId)
        {
            if (!_cacheConfig.Features.EnableLocationCache)
                return await _locationService.GetDistrictsAsync(governorateId);

            var cacheKey = governorateId.HasValue
                ? string.Format(CacheKeys.DistrictsByGovernorate, governorateId.Value)
                : "location:districts:all";

            var cached = await _cache.GetAsync<List<DistrictDto>>(cacheKey);

            if (cached != null)
            {
                _logger.LogDebug("Cache hit for districts");
                return cached;
            }

            var result = await _locationService.GetDistrictsAsync(governorateId);

            await _cache.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(_cacheConfig.Durations.LocationDataMinutes)
            );

            await _cache.TagAsync(cacheKey, CacheKeys.LocationTag);

            return result;
        }

        public async Task<AddressValidationResultDto> ValidateAddressAsync(ValidateAddressDto dto)
        {
            // Address validation shouldn't be cached as it's validation logic
            return await _locationService.ValidateAddressAsync(dto);
        }

        public async Task<District?> FindDistrictAsync(string governorateName, string districtName)
        {
            if (!_cacheConfig.Features.EnableLocationCache)
                return await _locationService.FindDistrictAsync(governorateName, districtName);

            // Create a cache key based on the search parameters
            var cacheKey = $"location:find:{governorateName.ToLower()}:{districtName.ToLower()}";
            var cached = await _cache.GetAsync<District>(cacheKey);

            if (cached != null)
            {
                _logger.LogDebug("Cache hit for district search");
                return cached;
            }

            var result = await _locationService.FindDistrictAsync(governorateName, districtName);

            if (result != null)
            {
                await _cache.SetAsync(
                    cacheKey,
                    result,
                    TimeSpan.FromMinutes(_cacheConfig.Durations.LocationDataMinutes)
                );

                await _cache.TagAsync(cacheKey, CacheKeys.LocationTag);
            }

            return result;
        }
    }
}