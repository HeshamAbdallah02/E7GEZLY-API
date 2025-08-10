using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.Services.Location;
using MediatR;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Location.Commands.GeocodeCoordinates
{
    /// <summary>
    /// Handler for geocoding coordinates to address
    /// </summary>
    public class GeocodeCoordinatesHandler : IRequestHandler<GeocodeCoordinatesCommand, ApplicationResult<GeocodeResponseDto>>
    {
        private readonly IGeocodingService _geocodingService;
        private readonly ILogger<GeocodeCoordinatesHandler> _logger;

        public GeocodeCoordinatesHandler(
            IGeocodingService geocodingService,
            ILogger<GeocodeCoordinatesHandler> logger)
        {
            _geocodingService = geocodingService;
            _logger = logger;
        }

        public async Task<ApplicationResult<GeocodeResponseDto>> Handle(
            GeocodeCoordinatesCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Geocoding coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);

                var result = await _geocodingService.GetAddressFromCoordinatesAsync(request.Latitude, request.Longitude);

                if (result == null)
                {
                    _logger.LogWarning("Could not geocode coordinates: {Latitude}, {Longitude}", 
                        request.Latitude, request.Longitude);
                    return ApplicationResult<GeocodeResponseDto>.Failure("Could not geocode location");
                }

                var addressResponse = new AddressResponseDto(
                    result.Latitude,
                    result.Longitude,
                    result.StreetName ?? result.FormattedAddress,
                    null, // Landmark
                    result.DistrictName,
                    result.DistrictName, // DistrictAr - using English for now
                    result.GovernorateName,
                    result.GovernorateName, // GovernorateAr - using English for now
                    result.FormattedAddress
                );

                var response = new GeocodeResponseDto
                {
                    Success = true,
                    Data = addressResponse,
                    Message = result.DistrictId.HasValue
                        ? "District identified successfully"
                        : "Location geocoded but district not matched"
                };

                _logger.LogInformation("Successfully geocoded coordinates to address");

                return ApplicationResult<GeocodeResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while geocoding coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);
                return ApplicationResult<GeocodeResponseDto>.Failure("Error geocoding coordinates");
            }
        }
    }
}