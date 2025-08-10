using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.DTOs.Location;
using MediatR;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Location.Queries.GetDistricts
{
    /// <summary>
    /// Handler for getting districts by governorate
    /// </summary>
    public class GetDistrictsHandler : IRequestHandler<GetDistrictsQuery, ApplicationResult<IEnumerable<DistrictDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ILogger<GetDistrictsHandler> _logger;

        public GetDistrictsHandler(
            ILocationRepository locationRepository,
            ILogger<GetDistrictsHandler> logger)
        {
            _locationRepository = locationRepository;
            _logger = logger;
        }

        public async Task<ApplicationResult<IEnumerable<DistrictDto>>> Handle(
            GetDistrictsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching districts for governorate {GovernorateId}", request.GovernorateId);

                var districts = await _locationRepository.GetDistrictsAsync(request.GovernorateId);

                var districtDtos = districts.Select(d => new DistrictDto
                {
                    Id = d.SystemId,
                    NameEn = d.NameEn,
                    NameAr = d.NameAr,
                    GovernorateId = d.GovernorateSystemId,
                    GovernorateName = d.Governorate?.NameEn ?? ""
                });

                _logger.LogInformation("Successfully fetched {Count} districts", districtDtos.Count());

                return ApplicationResult<IEnumerable<DistrictDto>>.Success(districtDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching districts for governorate {GovernorateId}", 
                    request.GovernorateId);
                return ApplicationResult<IEnumerable<DistrictDto>>.Failure("Failed to fetch districts");
            }
        }
    }
}