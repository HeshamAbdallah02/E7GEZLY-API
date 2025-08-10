using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.Location;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Location.Queries.GetDistrictFromCoordinates
{
    /// <summary>
    /// Handler for getting district from coordinates
    /// </summary>
    public class GetDistrictFromCoordinatesHandler : IRequestHandler<GetDistrictFromCoordinatesQuery, ApplicationResult<DistrictFromCoordinatesDto>>
    {
        private readonly IGeocodingService _geocodingService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetDistrictFromCoordinatesHandler> _logger;

        public GetDistrictFromCoordinatesHandler(
            IGeocodingService geocodingService,
            IApplicationDbContext context,
            ILogger<GetDistrictFromCoordinatesHandler> logger)
        {
            _geocodingService = geocodingService;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<DistrictFromCoordinatesDto>> Handle(
            GetDistrictFromCoordinatesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting district from coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);

                var districtId = await _geocodingService.GetDistrictIdFromCoordinatesAsync(request.Latitude, request.Longitude);

                if (districtId == null)
                {
                    _logger.LogWarning("Could not determine district from coordinates: {Latitude}, {Longitude}", 
                        request.Latitude, request.Longitude);
                    return ApplicationResult<DistrictFromCoordinatesDto>.Failure("Could not determine district from coordinates");
                }

                var district = await _context.Districts
                    .Include(d => d.Governorate)
                    .FirstOrDefaultAsync(d => d.Id == districtId.Value, cancellationToken);

                if (district == null)
                {
                    _logger.LogError("District {DistrictId} not found in database", districtId.Value);
                    return ApplicationResult<DistrictFromCoordinatesDto>.Failure("District not found in database");
                }

                var response = new DistrictFromCoordinatesDto
                {
                    Success = true,
                    DistrictId = districtId.Value,
                    District = new DistrictDetailsDto
                    {
                        Id = district.Id,
                        NameEn = district.NameEn,
                        NameAr = district.NameAr,
                        Governorate = new GovernorateDetailsDto
                        {
                            Id = district.Governorate.Id,
                            NameEn = district.Governorate.NameEn,
                            NameAr = district.Governorate.NameAr
                        }
                    }
                };

                _logger.LogInformation("Successfully found district {DistrictId} for coordinates", districtId.Value);

                return ApplicationResult<DistrictFromCoordinatesDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining district from coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);
                return ApplicationResult<DistrictFromCoordinatesDto>.Failure("Error determining district");
            }
        }
    }
}