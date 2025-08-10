using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.Login
{
    /// <summary>
    /// Handler for VenueLoginCommand using Clean Architecture with Domain layer
    /// </summary>
    public class VenueLoginHandler : IRequestHandler<VenueLoginCommand, ApplicationResult<VenueLoginResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IPasswordVerificationService _passwordVerificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VenueLoginHandler> _logger;
        private readonly UserManager<Models.ApplicationUser> _userManager; // Still needed for Identity operations
        private readonly SignInManager<Models.ApplicationUser> _signInManager; // Still needed for sign-in operations

        public VenueLoginHandler(
            IUserRepository userRepository,
            IVenueRepository venueRepository,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IPasswordVerificationService passwordVerificationService,
            IConfiguration configuration,
            ILogger<VenueLoginHandler> logger,
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager)
        {
            _userRepository = userRepository;
            _venueRepository = venueRepository;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _passwordVerificationService = passwordVerificationService;
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ApplicationResult<VenueLoginResponseDto>> Handle(VenueLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user using domain repository
                var user = await FindUserByEmailOrPhoneAsync(request.EmailOrPhone);

                if (user == null || user.VenueId == null)
                {
                    return ApplicationResult<VenueLoginResponseDto>.Failure("Invalid credentials");
                }

                // Still use Identity for password verification (will be abstracted later)
                var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
                if (appUser == null)
                {
                    return ApplicationResult<VenueLoginResponseDto>.Failure("Invalid credentials");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(appUser, request.Password, false);
                if (!result.Succeeded)
                {
                    return ApplicationResult<VenueLoginResponseDto>.Failure("Invalid credentials");
                }

                if (!user.IsActive)
                {
                    return ApplicationResult<VenueLoginResponseDto>.Failure("Account is deactivated");
                }

                // Check if user is verified (phone or email)
                if (!user.IsPhoneNumberVerified && !user.IsEmailVerified)
                {
                    return ApplicationResult<VenueLoginResponseDto>.Failure("Account not verified");
                }

                // Get venue using domain repository
                var venue = await _venueRepository.GetByIdAsync(user.VenueId.Value);
                if (venue == null)
                {
                    return ApplicationResult<VenueLoginResponseDto>.Failure("Venue not found");
                }

                _logger.LogInformation($"Venue logged in: {venue.Name.Value}");

                // Check if sub-user setup is required using domain logic
                var subUsers = await _venueRepository.GetSubUsersByVenueIdAsync(venue.Id);
                var hasSubUsers = subUsers.Any();

                if (!hasSubUsers && venue.IsProfileComplete)
                {
                    // Use domain method to update venue
                    venue.SetRequiresSubUserSetup(true);
                    await _venueRepository.UpdateAsync(venue);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // Generate gateway token
                var gatewayToken = GenerateVenueGatewayToken(venue.Id, user.Id.ToString());

                // Build response using domain entities
                var response = new VenueLoginResponseDto
                {
                    GatewayToken = gatewayToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    RequiresSubUserSetup = venue.RequiresSubUserSetup,
                    User = new UserAuthInfoDto(
                        Id: user.Id.ToString(),
                        Email: user.Email,
                        PhoneNumber: user.PhoneNumber,
                        IsPhoneVerified: user.IsPhoneNumberVerified,
                        IsEmailVerified: user.IsEmailVerified
                    ),
                    Venue = new VenueAuthInfoDto(
                        Id: venue.Id,
                        Name: venue.Name.Value,
                        Type: venue.VenueType.ToString(),
                        IsProfileComplete: venue.IsProfileComplete,
                        Location: venue.IsProfileComplete && venue.Address != null ? new
                        {
                            latitude = venue.Address.Coordinates?.Latitude,
                            longitude = venue.Address.Coordinates?.Longitude,
                            streetAddress = venue.Address.StreetAddress,
                            district = "District Name", // Get from venue.Address
                            governorate = "Governorate Name", // Get from venue.Address
                            fullAddress = venue.Address.ToString()
                        } : null
                    ),
                    RequiredActions = GetRequiredActions(user, venue),
                    Metadata = GetAuthMetadata(venue),
                    NextStep = venue.RequiresSubUserSetup ? "create-first-admin" : "sub-user-login"
                };

                return ApplicationResult<VenueLoginResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during venue login");
                return ApplicationResult<VenueLoginResponseDto>.Failure("An error occurred during login");
            }
        }

        private async Task<Domain.Entities.User?> FindUserByEmailOrPhoneAsync(string emailOrPhone)
        {
            Domain.Entities.User? user;

            // Check if input is email
            if (emailOrPhone.Contains('@'))
            {
                user = await _userRepository.GetByEmailAsync(emailOrPhone);
            }
            else
            {
                // Format phone number
                var formattedPhone = FormatPhoneNumber(emailOrPhone);
                user = await _userRepository.GetByPhoneAsync(formattedPhone);
            }

            return user;
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove any spaces or special characters
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Add +20 if not present
            if (!cleaned.StartsWith("+"))
            {
                if (cleaned.StartsWith("20"))
                {
                    cleaned = "+" + cleaned;
                }
                else if (cleaned.StartsWith("0"))
                {
                    cleaned = "+20" + cleaned.Substring(1);
                }
                else
                {
                    cleaned = "+20" + cleaned;
                }
            }

            return cleaned;
        }

        private List<string> GetRequiredActions(Domain.Entities.User user, Domain.Entities.Venue venue)
        {
            var actions = new List<string>();

            if (!venue.IsProfileComplete)
                actions.Add("COMPLETE_PROFILE");

            return actions;
        }

        private AuthMetadataDto? GetAuthMetadata(Domain.Entities.Venue venue)
        {
            if (!venue.IsProfileComplete)
            {
                return new AuthMetadataDto(
                    ProfileCompletionUrl: venue.VenueType switch
                    {
                        Domain.Enums.VenueType.PlayStationVenue => "/api/venue/profile/complete/playstation",
                        Domain.Enums.VenueType.FootballCourt => "/api/venue/profile/complete/court",
                        Domain.Enums.VenueType.PadelCourt => "/api/venue/profile/complete/court",
                        _ => "/api/venue/profile/complete"
                    },
                    NextStepDescription: "Complete your venue profile to start receiving bookings",
                    AdditionalData: new Dictionary<string, object>
                    {
                        ["venueType"] = venue.VenueType.ToString(),
                        ["estimatedCompletionTime"] = "5-10 minutes"
                    }
                );
            }

            return null;
        }

        private string GenerateVenueGatewayToken(Guid venueId, string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("venueId", venueId.ToString()),
                new Claim("type", "venue-gateway")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(24); // Gateway tokens last longer

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}