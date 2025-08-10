using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.DTOs.Location;
using MediatR;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Location.Queries.GetGovernorates
{
    /// <summary>
    /// Handler for getting all governorates
    /// </summary>
    public class GetGovernoratesHandler : IRequestHandler<GetGovernoratesQuery, ApplicationResult<IEnumerable<GovernorateDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ILogger<GetGovernoratesHandler> _logger;

        public GetGovernoratesHandler(
            ILocationRepository locationRepository,
            ILogger<GetGovernoratesHandler> logger)
        {
            _locationRepository = locationRepository;
            _logger = logger;
        }

        public async Task<ApplicationResult<IEnumerable<GovernorateDto>>> Handle(
            GetGovernoratesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching all governorates");

                var governorates = await _locationRepository.GetGovernoratesAsync();

                var governorateDtos = governorates.Select(g => new GovernorateDto(
                    g.SystemId,
                    g.NameEn,
                    g.NameAr
                ));

                _logger.LogInformation("Successfully fetched {Count} governorates", governorateDtos.Count());

                return ApplicationResult<IEnumerable<GovernorateDto>>.Success(governorateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching governorates");
                return ApplicationResult<IEnumerable<GovernorateDto>>.Failure("Failed to fetch governorates");
            }
        }
    }
}