// Controllers/Auth/VenueAuthController.cs
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using E7GEZLY_API.Services.VenueManagement;
using E7GEZLY_API.Application.Features.Authentication.Commands.Register;
using E7GEZLY_API.Application.Features.Authentication.Commands.Login;
using E7GEZLY_API.Application.Features.Authentication.Commands.CreateFirstAdmin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E7GEZLY_API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth/venue")]
    public class VenueAuthController : BaseAuthController
    {
        private readonly IMediator _mediator;
        
        public VenueAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<VenueAuthController> logger,
            IWebHostEnvironment environment,
            IMediator mediator)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        [RateLimit(2, 3600, "Venue registration rate limit exceeded. You can only register 2 venues per hour.")]
        public async Task<IActionResult> RegisterVenue(RegisterVenueDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var command = new RegisterVenueCommand
                {
                    Email = dto.Email,
                    Password = dto.Password,
                    PhoneNumber = dto.PhoneNumber,
                    VenueName = dto.VenueName,
                    VenueType = dto.VenueType
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                
                return BadRequest(new { message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during venue registration");

                if (_environment.IsDevelopment())
                {
                    return StatusCode(500, new
                    {
                        message = "An error occurred during registration",
                        detail = ex.Message,
                        stackTrace = ex.StackTrace,
                        innerException = ex.InnerException?.Message
                    });
                }

                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }


        /// <summary>
        /// Login as venue (gateway only)
        /// </summary>
        [HttpPost("login")]
        [RateLimit(3, 300, "Venue login rate limit exceeded. Too many login attempts. Please wait 5 minutes.")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var command = new VenueLoginCommand
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
                    return Ok(result.Data);
                }
                
                return Unauthorized(new { message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during venue login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Create first admin after profile completion
        /// </summary>
        [HttpPost("create-first-admin")]
        [Authorize(Policy = "VenueGateway")]
        public async Task<IActionResult> CreateFirstAdmin([FromBody] CreateVenueSubUserDto dto)
        {
            try
            {
                var venueId = User.GetVenueId();

                var command = new CreateFirstAdminCommand
                {
                    VenueId = venueId,
                    Username = dto.Username,
                    Password = dto.Password
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                
                return BadRequest(new { message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first admin");
                return StatusCode(500, new { message = "An error occurred while creating the first admin" });
            }
        }

    }
}