using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Controllers
{
    [ApiController]
    [Route("api/locations")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ILogger<LocationController> _logger;
        private readonly IGeocodingService _geocodingService;
        private readonly AppDbContext _context;

        public LocationController(ILocationService locationService, IGeocodingService geocodingService, ILogger<LocationController> logger, AppDbContext context)
        {
            _locationService = locationService;
            _geocodingService = geocodingService;
            _logger = logger;
            _context = context;
        }

        [HttpGet("governorates")]
        public async Task<IActionResult> GetGovernorates()
        {
            try
            {
                var governorates = await _locationService.GetGovernoratesAsync();
                return Ok(governorates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching governorates");
                return StatusCode(500, new { message = "Error fetching governorates" });
            }
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int? governorateId)
        {
            try
            {
                var districts = await _locationService.GetDistrictsAsync(governorateId);
                return Ok(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching districts");
                return StatusCode(500, new { message = "Error fetching districts" });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateAddress([FromBody] ValidateAddressDto dto)
        {
            try
            {
                var result = await _locationService.ValidateAddressAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating address");
                return StatusCode(500, new { message = "Error validating address" });
            }
        }

        [HttpPost("geocode")]
        public async Task<IActionResult> GeocodeCoordinates([FromBody] GeocodeRequestDto dto)
        {
            try
            {
                var result = await _geocodingService.GetAddressFromCoordinatesAsync(dto.Latitude, dto.Longitude);

                if (result == null)
                {
                    return NotFound(new { message = "Could not geocode location" });
                }

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = result.DistrictId.HasValue
                        ? "District identified successfully"
                        : "Location geocoded but district not matched"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding coordinates");
                return StatusCode(500, new { message = "Error geocoding coordinates" });
            }
        }

        [HttpPost("geocode/district")]
        public async Task<IActionResult> GetDistrictFromCoordinates([FromBody] GeocodeRequestDto dto)
        {
            try
            {
                var districtId = await _geocodingService.GetDistrictIdFromCoordinatesAsync(dto.Latitude, dto.Longitude);

                if (districtId == null)
                {
                    return NotFound(new { message = "Could not determine district from coordinates" });
                }

                var district = await _context.Districts
                   .Include(d => d.Governorate)
                   .FirstOrDefaultAsync(d => d.Id == districtId.Value);

                if (district == null)
                {
                    return NotFound(new { message = "District not found in database" });
                }

                return Ok(new
                {
                    success = true,
                    districtId = districtId.Value,
                    district = new
                    {
                        id = district!.Id,
                        nameEn = district.NameEn,
                        nameAr = district.NameAr,
                        governorate = new
                        {
                            id = district.Governorate.Id,
                            nameEn = district.Governorate.NameEn,
                            nameAr = district.Governorate.NameAr
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining district from coordinates");
                return StatusCode(500, new { message = "Error determining district" });
            }
        }

        public record GeocodeRequestDto(double Latitude, double Longitude);
    }
}