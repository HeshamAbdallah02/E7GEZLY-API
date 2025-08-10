using E7GEZLY_API.Application.Features.Location.Commands.GeocodeCoordinates;
using E7GEZLY_API.Application.Features.Location.Commands.ValidateAddress;
using E7GEZLY_API.Application.Features.Location.Queries.GetDistricts;
using E7GEZLY_API.Application.Features.Location.Queries.GetDistrictFromCoordinates;
using E7GEZLY_API.Application.Features.Location.Queries.GetGovernorates;
using E7GEZLY_API.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers
{
    /// <summary>
    /// Location controller using Clean Architecture with MediatR
    /// </summary>
    [ApiController]
    [Route("api/locations")]
    public class LocationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<LocationController> _logger;

        public LocationController(IMediator mediator, ILogger<LocationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get all governorates
        /// </summary>
        /// <returns>List of governorates</returns>
        [HttpGet("governorates")]
        public async Task<IActionResult> GetGovernorates()
        {
            _logger.LogInformation("Received request to get all governorates");

            var query = new GetGovernoratesQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<object>.CreateSuccess(result.Data, "Governorates retrieved successfully"));
            }

            _logger.LogWarning("Failed to retrieve governorates: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<object>.CreateError(result.ErrorMessage));
        }

        /// <summary>
        /// Get districts by governorate (optional)
        /// </summary>
        /// <param name="governorateId">Optional governorate ID filter</param>
        /// <returns>List of districts</returns>
        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int? governorateId)
        {
            _logger.LogInformation("Received request to get districts for governorate {GovernorateId}", governorateId);

            var query = new GetDistrictsQuery(governorateId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<object>.CreateSuccess(result.Data, "Districts retrieved successfully"));
            }

            _logger.LogWarning("Failed to retrieve districts: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<object>.CreateError(result.ErrorMessage));
        }

        /// <summary>
        /// Validate an address
        /// </summary>
        /// <param name="dto">Address validation data</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateAddress([FromBody] ValidateAddressCommand command)
        {
            _logger.LogInformation("Received request to validate address");

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<object>.CreateSuccess(result.Data, "Address validated successfully"));
            }

            _logger.LogWarning("Address validation failed: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<object>.CreateError(result.ErrorMessage));
        }

        /// <summary>
        /// Geocode coordinates to address
        /// </summary>
        /// <param name="dto">Geocoding request data</param>
        /// <returns>Geocoded address information</returns>
        [HttpPost("geocode")]
        public async Task<IActionResult> GeocodeCoordinates([FromBody] GeocodeRequestDto dto)
        {
            _logger.LogInformation("Received request to geocode coordinates {Latitude}, {Longitude}", 
                dto.Latitude, dto.Longitude);

            var command = new GeocodeCoordinatesCommand(dto.Latitude, dto.Longitude);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            if (result.ErrorMessage == "Could not geocode location")
            {
                return NotFound(ApiResponse<object>.CreateError("Could not geocode location"));
            }

            _logger.LogWarning("Geocoding failed: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<object>.CreateError(result.ErrorMessage));
        }

        /// <summary>
        /// Get district information from coordinates
        /// </summary>
        /// <param name="dto">Coordinate data</param>
        /// <returns>District information</returns>
        [HttpPost("geocode/district")]
        public async Task<IActionResult> GetDistrictFromCoordinates([FromBody] GeocodeRequestDto dto)
        {
            _logger.LogInformation("Received request to get district from coordinates {Latitude}, {Longitude}", 
                dto.Latitude, dto.Longitude);

            var query = new GetDistrictFromCoordinatesQuery(dto.Latitude, dto.Longitude);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            if (result.ErrorMessage == "Could not determine district from coordinates")
            {
                return NotFound(ApiResponse<object>.CreateError("Could not determine district from coordinates"));
            }

            if (result.ErrorMessage == "District not found in database")
            {
                return NotFound(ApiResponse<object>.CreateError("District not found in database"));
            }

            _logger.LogWarning("Failed to get district from coordinates: {Error}", result.ErrorMessage);
            return StatusCode(500, ApiResponse<object>.CreateError("Error determining district"));
        }

        /// <summary>
        /// Geocoding request DTO
        /// </summary>
        public record GeocodeRequestDto(double Latitude, double Longitude);
    }
}