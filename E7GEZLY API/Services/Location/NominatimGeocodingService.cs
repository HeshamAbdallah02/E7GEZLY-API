// Services/Location/NominatimGeocodingService.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.Exceptions;
using E7GEZLY_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace E7GEZLY_API.Services.Location
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ILogger<NominatimGeocodingService> _logger;
        private readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
        private DateTime _lastRequest = DateTime.MinValue;

        public NominatimGeocodingService(
            HttpClient httpClient,
            AppDbContext context,
            ILogger<NominatimGeocodingService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;

            // Set user agent as required by Nominatim
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "E7GEZLY/1.0 (contact@e7gezly.com)");
        }

        public async Task<GeocodingResult?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            // Validate coordinates
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                throw new GeocodingException(
                    "Invalid coordinates provided",
                    GeocodingErrorType.InvalidCoordinates,
                    latitude,
                    longitude
                );
            }

            await _rateLimiter.WaitAsync();
            try
            {
                // Ensure we respect the 1 request per second rate limit
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequest;
                if (timeSinceLastRequest.TotalMilliseconds < 1000)
                {
                    await Task.Delay(1000 - (int)timeSinceLastRequest.TotalMilliseconds);
                }

                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=16&addressdetails=1&accept-language=en";

                _logger.LogInformation($"Calling Nominatim API for coordinates: {latitude}, {longitude}");

                var response = await _httpClient.GetAsync(url);
                _lastRequest = DateTime.UtcNow;

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new GeocodingException(
                        "Rate limit exceeded for geocoding service",
                        GeocodingErrorType.RateLimitExceeded,
                        latitude,
                        longitude
                    );
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Nominatim API returned {response.StatusCode} for coordinates {latitude}, {longitude}");
                    throw new GeocodingException(
                        $"Geocoding service returned error: {response.StatusCode}",
                        GeocodingErrorType.ServiceUnavailable,
                        latitude,
                        longitude
                    );
                }

                // Process response...
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);
                var root = data.RootElement;

                // Check if we got an error response
                if (root.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetString() ?? "Unknown error";
                    _logger.LogWarning($"Nominatim returned error: {errorMessage}");
                    return null;
                }

                var result = new GeocodingResult
                {
                    FormattedAddress = root.GetProperty("display_name").GetString(),
                    Latitude = latitude,
                    Longitude = longitude
                };

                // Process address details...
                if (root.TryGetProperty("address", out var address))
                {
                    ProcessAddressComponents(address, result);

                    // Try to match with our districts
                    var matchedDistrict = await FindMatchingDistrict(result);
                    if (matchedDistrict != null)
                    {
                        result.DistrictId = matchedDistrict.Id;
                        result.DistrictName = matchedDistrict.NameEn;
                        result.GovernorateName = matchedDistrict.Governorate.NameEn;
                        result.MatchConfidence = CalculateMatchConfidence(result, matchedDistrict);
                    }
                }

                _logger.LogInformation($"Geocoding successful: District={result.DistrictName}, Confidence={result.MatchConfidence}");
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during geocoding");
                throw new GeocodingException(
                    "Network error while contacting geocoding service",
                    GeocodingErrorType.NetworkError,
                    latitude,
                    longitude
                );
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Nominatim API request timed out");
                throw new GeocodingException(
                    "Geocoding request timed out",
                    GeocodingErrorType.ServiceUnavailable,
                    latitude,
                    longitude
                );
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private void ProcessAddressComponents(JsonElement address, GeocodingResult result)
        {
            // Extract all possible address components
            var addressFields = new Dictionary<string, Action<string>>
    {
        { "suburb", value => result.Suburb = value },
        { "district", value => result.DistrictName = value },
        { "city", value => result.City = value },
        { "state", value => result.State = value },
        { "road", value => result.StreetName = value },
        { "neighbourhood", value => result.Neighbourhood = value },
        { "county", value => result.County = value },
        { "postcode", value => result.PostCode = value }
    };

            foreach (var field in addressFields)
            {
                if (address.TryGetProperty(field.Key, out var element))
                {
                    var value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        field.Value(value);
                    }
                }
            }
        }

        private double CalculateMatchConfidence(GeocodingResult result, District district)
        {
            double confidence = 0;

            // Exact name match
            if (result.DistrictName?.Equals(district.NameEn, StringComparison.OrdinalIgnoreCase) == true ||
                result.Suburb?.Equals(district.NameEn, StringComparison.OrdinalIgnoreCase) == true)
            {
                confidence += 0.5;
            }
            // Partial match
            else if (result.DistrictName?.Contains(district.NameEn, StringComparison.OrdinalIgnoreCase) == true ||
                     district.NameEn.Contains(result.DistrictName ?? "", StringComparison.OrdinalIgnoreCase))
            {
                confidence += 0.3;
            }

            // Check if coordinates are close to district center
            if (district.CenterLatitude.HasValue && district.CenterLongitude.HasValue)
            {
                var distance = CalculateDistance(
                    result.Latitude ?? 0,
                    result.Longitude ?? 0,
                    district.CenterLatitude.Value,
                    district.CenterLongitude.Value
                );

                if (distance < 2) confidence += 0.3;      // Very close
                else if (distance < 5) confidence += 0.2; // Close
                else if (distance < 10) confidence += 0.1; // Somewhat close
            }

            // State/Governorate match
            if (result.State != null && district.Governorate != null)
            {
                if (MapToGovernorate(result.State)?.Equals(district.Governorate.NameEn, StringComparison.OrdinalIgnoreCase) == true)
                {
                    confidence += 0.2;
                }
            }

            return Math.Min(confidence, 1.0);
        }

        public async Task<int?> GetDistrictIdFromCoordinatesAsync(double latitude, double longitude)
        {
            var result = await GetAddressFromCoordinatesAsync(latitude, longitude);

            if (result?.DistrictId != null)
            {
                _logger.LogInformation($"District found via geocoding: {result.DistrictName} (ID: {result.DistrictId})");
                return result.DistrictId;
            }

            // Fallback: Find nearest district by coordinates
            _logger.LogWarning("Geocoding did not find a district match, attempting nearest district calculation");
            return await FindNearestDistrictByCoordinates(latitude, longitude);
        }

        private async Task<Models.District?> FindMatchingDistrict(GeocodingResult geocodingResult)
        {
            // Priority 1: Try exact match with suburb
            if (!string.IsNullOrEmpty(geocodingResult.Suburb))
            {
                var district = await _context.Districts
                    .Include(d => d.Governorate)
                    .FirstOrDefaultAsync(d =>
                        d.NameEn.ToLower() == geocodingResult.Suburb.ToLower() ||
                        d.NameAr == geocodingResult.Suburb ||
                        d.NameEn.ToLower().Contains(geocodingResult.Suburb.ToLower()) ||
                        geocodingResult.Suburb.ToLower().Contains(d.NameEn.ToLower()));

                if (district != null) return district;
            }

            // Priority 2: Try district name
            if (!string.IsNullOrEmpty(geocodingResult.DistrictName))
            {
                var district = await _context.Districts
                    .Include(d => d.Governorate)
                    .FirstOrDefaultAsync(d =>
                        d.NameEn.ToLower() == geocodingResult.DistrictName.ToLower() ||
                        d.NameAr == geocodingResult.DistrictName);

                if (district != null) return district;
            }

            // Priority 3: Try to match by city/state combination
            if (!string.IsNullOrEmpty(geocodingResult.City) && !string.IsNullOrEmpty(geocodingResult.State))
            {
                // Map common Nominatim names to our governorates
                var governorateName = MapToGovernorate(geocodingResult.State);

                if (governorateName != null)
                {
                    var district = await _context.Districts
                        .Include(d => d.Governorate)
                        .FirstOrDefaultAsync(d =>
                            d.Governorate.NameEn.ToLower() == governorateName.ToLower() &&
                            (d.NameEn.ToLower().Contains(geocodingResult.City.ToLower()) ||
                             geocodingResult.City.ToLower().Contains(d.NameEn.ToLower())));

                    if (district != null) return district;
                }
            }

            return null;
        }

        private async Task<int?> FindNearestDistrictByCoordinates(double latitude, double longitude)
        {
            // Define approximate center points for major districts
            var districtCenters = new Dictionary<int, (double lat, double lng, string name)>
            {
                // Cairo Districts
                { 1, (30.0626, 31.2497, "Nasr City") },
                { 2, (30.0131, 31.2089, "Maadi") },
                { 3, (30.0609, 31.2196, "Heliopolis") },
                { 4, (30.0489, 31.2625, "New Cairo") },
                { 5, (30.0595, 31.2026, "Zamalek") },
                
                // Giza Districts
                { 11, (29.9285, 30.9188, "6th of October") },
                { 12, (30.0176, 31.0158, "Sheikh Zayed") },
                { 13, (30.0084, 31.2085, "Haram") },
                
                // Add more as needed
            };

            double minDistance = double.MaxValue;
            int? nearestDistrictId = null;

            foreach (var center in districtCenters)
            {
                var distance = CalculateDistance(latitude, longitude, center.Value.lat, center.Value.lng);

                // Only consider districts within 20km
                if (distance < 20 && distance < minDistance)
                {
                    // Verify the district exists in database
                    var exists = await _context.Districts.AnyAsync(d => d.Id == center.Key);
                    if (exists)
                    {
                        minDistance = distance;
                        nearestDistrictId = center.Key;
                    }
                }
            }

            if (nearestDistrictId.HasValue)
            {
                _logger.LogInformation($"Found nearest district ID {nearestDistrictId} at {minDistance:F2}km distance");
            }
            else
            {
                _logger.LogWarning($"No district found within 20km of coordinates {latitude}, {longitude}");
            }

            return nearestDistrictId;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radius of Earth in km
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRad(double deg) => deg * (Math.PI / 180);

        private string? MapToGovernorate(string nominatimState)
        {
            // Map common Nominatim state/governorate names to our database names
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Cairo Governorate", "Cairo" },
                { "القاهرة", "Cairo" },
                { "Giza Governorate", "Giza" },
                { "الجيزة", "Giza" },
                { "Alexandria Governorate", "Alexandria" },
                { "الإسكندرية", "Alexandria" },
                { "Qalyubia Governorate", "Qalyubia" },
                { "القليوبية", "Qalyubia" },
                { "Dakahlia Governorate", "Dakahlia" },
                { "الدقهلية", "Dakahlia" },
                // Add more mappings as needed
            };

            return mappings.TryGetValue(nominatimState, out var governorate) ? governorate : nominatimState;
        }
    }
}