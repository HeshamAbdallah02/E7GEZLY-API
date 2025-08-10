using E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteProfile;
using E7GEZLY_API.Application.Features.VenueProfile.Queries.GetVenueProfile;
using E7GEZLY_API.Application.Features.VenueProfile.Queries.IsProfileComplete;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.VenueManagement
{
    /// <summary>
    /// Venue Profile Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles venue profile management operations through Application layer
    /// </summary>
    [ApiController]
    [Route("api/venue/profile")]
    [Authorize]
    [RequireVenueGateway]
    public class VenueProfileController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<VenueProfileController> _logger;

        public VenueProfileController(IMediator mediator, ILogger<VenueProfileController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get venue profile information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<VenueProfileDto>>> GetVenueProfile()
        {
            try
            {
                var venueId = HttpContext.GetVenueId();
                if (!venueId.HasValue)
                {
                    return BadRequest(ApiResponse<VenueProfileDto>.CreateError("Venue ID not found in token"));
                }

                var query = new GetVenueProfileQuery { VenueId = venueId.Value };
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<VenueProfileDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<VenueProfileDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting venue profile");
                return StatusCode(500, ApiResponse<VenueProfileDto>.CreateError("An error occurred while retrieving the venue profile"));
            }
        }

        /// <summary>
        /// Complete venue profile (Clean Architecture approach)
        /// </summary>
        [HttpPost("complete")]
        public async Task<ActionResult<ApiResponse<VenueProfileCompletionResponseDto>>> CompleteProfile([FromBody] CompleteVenueProfileRequest request)
        {
            try
            {
                var venueId = HttpContext.GetVenueId();
                if (!venueId.HasValue)
                {
                    return BadRequest(ApiResponse<VenueProfileCompletionResponseDto>.CreateError("Venue ID not found in token"));
                }

                var command = new CompleteVenueProfileCommand
                {
                    VenueId = venueId.Value,
                    StreetAddress = request.StreetAddress,
                    Landmark = request.Landmark,
                    DistrictId = request.DistrictId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Description = request.Description,
                    WorkingHours = request.WorkingHours,
                    Pricing = request.Pricing,
                    ImageUrls = request.ImageUrls,
                    PlayStationDetails = request.PlayStationDetails
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Venue profile completed successfully for venue {VenueId}", venueId.Value);
                    return Ok(ApiResponse<VenueProfileCompletionResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<VenueProfileCompletionResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing venue profile");
                return StatusCode(500, ApiResponse<VenueProfileCompletionResponseDto>.CreateError("An error occurred while completing the profile"));
            }
        }

        /// <summary>
        /// Check if venue profile is complete
        /// </summary>
        [HttpGet("is-complete")]
        public async Task<ActionResult<ApiResponse<bool>>> IsProfileComplete()
        {
            try
            {
                var venueId = HttpContext.GetVenueId();
                if (!venueId.HasValue)
                {
                    return BadRequest(ApiResponse<bool>.CreateError("Venue ID not found in token"));
                }

                var query = new IsProfileCompleteQuery { VenueId = venueId.Value };
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<ProfileCompletionStatusDto>.CreateSuccess(result.Data));
                }

                return BadRequest(ApiResponse<ProfileCompletionStatusDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking profile completion status");
                return StatusCode(500, ApiResponse<bool>.CreateError("An error occurred while checking profile completion status"));
            }
        }
    }

    /// <summary>
    /// Request DTO for completing venue profile
    /// </summary>
    public class CompleteVenueProfileRequest
    {
        public string StreetAddress { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public int DistrictId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Description { get; set; }
        public List<VenueWorkingHoursDto> WorkingHours { get; set; } = new();
        public List<VenuePricingDto> Pricing { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public VenuePlayStationDetailsDto? PlayStationDetails { get; set; }
    }
}