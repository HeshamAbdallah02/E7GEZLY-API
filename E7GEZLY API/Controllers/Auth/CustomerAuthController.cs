// Controllers/Auth/CustomerAuthController.cs
using E7GEZLY_API.Application.Features.Authentication.Commands.CustomerLogin;
using E7GEZLY_API.Application.Features.Authentication.Commands.RegisterCustomer;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace E7GEZLY_API.Controllers.Auth
{
    /// <summary>
    /// Customer Authentication Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles customer registration and login through Application layer
    /// </summary>
    [ApiController]
    [Route("api/auth/customer")]
    public class CustomerAuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CustomerAuthController> _logger;

        public CustomerAuthController(IMediator mediator, ILogger<CustomerAuthController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Register a new customer
        /// </summary>
        [HttpPost("register")]
        [RateLimit(3, 3600, "Registration rate limit exceeded. You can only register 3 accounts per hour.")]
        public async Task<ActionResult<ApiResponse<RegistrationResponseDto>>> RegisterCustomer(RegisterCustomerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<RegistrationResponseDto>.CreateError("Validation failed"));
                }

                var command = new RegisterCustomerCommand
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Password = dto.Password,
                    DateOfBirth = dto.DateOfBirth,
                    Governorate = dto.Address?.Governorate,
                    District = dto.Address?.District,
                    StreetAddress = dto.Address?.StreetAddress,
                    Landmark = dto.Address?.Landmark,
                    Latitude = dto.Address?.Latitude,
                    Longitude = dto.Address?.Longitude
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Customer registration successful");
                    return Ok(ApiResponse<RegistrationResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<RegistrationResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer registration");
                return StatusCode(500, ApiResponse<RegistrationResponseDto>.CreateError("An error occurred during registration"));
            }
        }

        /// <summary>
        /// Customer login
        /// </summary>
        [HttpPost("login")]
        [RateLimit(5, 300, "Login rate limit exceeded. Too many login attempts. Please wait 5 minutes.")]
        public async Task<ActionResult<ApiResponse<object>>> CustomerLogin([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.CreateError("Validation failed"));
                }

                var command = new CustomerLoginCommand
                {
                    EmailOrPhone = dto.EmailOrPhone,
                    Password = dto.Password,
                    DeviceName = HttpContext.GetDeviceName(),
                    DeviceType = HttpContext.DetectDeviceType(),
                    UserAgent = Request.Headers["User-Agent"].FirstOrDefault(),
                    IpAddress = HttpContext.GetClientIpAddress()
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Customer login successful");
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                // Check if it's a verification error
                if (result.ErrorMessage!.Contains("not verified"))
                {
                    var errorParts = result.Errors.Where(e => e.StartsWith("UserId:") || e.StartsWith("RequiresVerification:")).ToList();
                    var userId = errorParts.FirstOrDefault(e => e.StartsWith("UserId:"))?.Split(':')[1];
                    var requiresVerification = errorParts.FirstOrDefault(e => e.StartsWith("RequiresVerification:"))?.Split(':')[1] == "true";
                    
                    return Unauthorized(ApiResponse<object>.CreateError(result.ErrorMessage));
                }

                return Unauthorized(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer login");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred during login"));
            }
        }

    }
}