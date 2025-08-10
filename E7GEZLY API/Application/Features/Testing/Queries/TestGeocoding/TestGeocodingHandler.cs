using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.Location;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Testing.Queries.TestGeocoding
{
    /// <summary>
    /// Handler for testing geocoding functionality
    /// </summary>
    public class TestGeocodingHandler : IRequestHandler<TestGeocodingQuery, ApplicationResult<GeocodingTestResponse>>
    {
        private readonly IGeocodingService _geocodingService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TestGeocodingHandler> _logger;

        public TestGeocodingHandler(
            IGeocodingService geocodingService,
            IApplicationDbContext context,
            ILogger<TestGeocodingHandler> logger)
        {
            _geocodingService = geocodingService;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<GeocodingTestResponse>> Handle(
            TestGeocodingQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Testing geocoding for coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);

                // Test 1: Get full address info
                var addressInfo = await _geocodingService.GetAddressFromCoordinatesAsync(request.Latitude, request.Longitude);

                // Test 2: Get district ID
                var districtId = await _geocodingService.GetDistrictIdFromCoordinatesAsync(request.Latitude, request.Longitude);

                // Get district details if found
                DistrictTestInfo? districtDetails = null;
                if (districtId.HasValue)
                {
                    var district = await _context.Districts
                        .Include(d => d.Governorate)
                        .FirstOrDefaultAsync(d => d.Id == districtId.Value, cancellationToken);

                    if (district != null)
                    {
                        districtDetails = new DistrictTestInfo
                        {
                            Id = district.Id,
                            NameEn = district.NameEn,
                            NameAr = district.NameAr,
                            Governorate = district.Governorate.NameEn
                        };
                    }
                }

                var response = new GeocodingTestResponse
                {
                    Coordinates = new CoordinateInfo(request.Latitude, request.Longitude),
                    AddressInfo = addressInfo,
                    DistrictId = districtId,
                    DistrictDetails = districtDetails,
                    Success = districtId.HasValue
                };

                _logger.LogInformation("Geocoding test completed successfully. District found: {Found}", districtId.HasValue);

                return ApplicationResult<GeocodingTestResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during geocoding test for coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);

                var errorResponse = new GeocodingTestResponse
                {
                    Coordinates = new CoordinateInfo(request.Latitude, request.Longitude),
                    AddressInfo = new { error = ex.Message, type = ex.GetType().Name },
                    Success = false
                };

                return ApplicationResult<GeocodingTestResponse>.Success(errorResponse);
            }
        }
    }
}