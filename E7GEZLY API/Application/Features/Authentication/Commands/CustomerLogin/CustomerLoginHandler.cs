using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.CustomerLogin
{
    /// <summary>
    /// Handler for CustomerLoginCommand
    /// </summary>
    public class CustomerLoginHandler : IRequestHandler<CustomerLoginCommand, ApplicationResult<object>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<CustomerLoginHandler> _logger;

        public CustomerLoginHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IApplicationDbContext context,
            ITokenService tokenService,
            ILogger<CustomerLoginHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(CustomerLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user by email or phone
                var user = await FindUserByEmailOrPhoneAsync(request.EmailOrPhone, cancellationToken);

                if (user == null || user.VenueId != null) // Ensure it's a customer
                {
                    return ApplicationResult<object>.Failure("Invalid credentials");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return ApplicationResult<object>.Failure("Invalid credentials");
                }

                if (!user.IsActive)
                {
                    return ApplicationResult<object>.Failure("Account is deactivated");
                }

                // Check if user is verified
                if (!user.IsPhoneNumberVerified && !user.IsEmailVerified)
                {
                    return ApplicationResult<object>.Failure("Account not verified", 
                        $"UserId:{user.Id}", "RequiresVerification:true");
                }

                _logger.LogInformation($"Customer logged in: {user.Email}");

                // Create session info
                var sessionInfo = new CreateSessionDto(
                    DeviceName: request.DeviceName ?? "Unknown Device",
                    DeviceType: request.DeviceType ?? "Unknown",
                    UserAgent: request.UserAgent,
                    IpAddress: request.IpAddress ?? "Unknown"
                );

                var tokens = await _tokenService.GenerateTokensAsync(user, sessionInfo);

                // Get customer profile with location
                var customerProfile = await _context.CustomerProfiles
                    .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                    .FirstOrDefaultAsync(cp => cp.UserId == user.Id, cancellationToken);

                var requiredActions = GetCustomerRequiredActions(user, customerProfile);
                var metadata = GetCustomerAuthMetadata(user, customerProfile);

                var response = new
                {
                    success = true,
                    tokens = tokens,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        isPhoneVerified = user.IsPhoneNumberVerified,
                        isEmailVerified = user.IsEmailVerified
                    },
                    profile = customerProfile != null ? new
                    {
                        id = customerProfile.Id,
                        firstName = customerProfile.Name.FirstName,
                        lastName = customerProfile.Name.LastName,
                        dateOfBirth = customerProfile.DateOfBirth,
                        fullAddress = customerProfile.GetFullAddress(),
                        location = customerProfile.DistrictSystemId.HasValue ? new
                        {
                            districtId = customerProfile.DistrictSystemId,
                            districtName = customerProfile.District?.NameEn,
                            districtNameAr = customerProfile.District?.NameAr,
                            governorateId = customerProfile.District?.GovernorateId,
                            governorateName = customerProfile.District?.Governorate?.NameEn,
                            governorateNameAr = customerProfile.District?.Governorate?.NameAr,
                            streetAddress = customerProfile.Address.StreetAddress,
                            landmark = customerProfile.Address.Landmark
                        } : null
                    } : null,
                    requiredActions = requiredActions,
                    metadata = metadata
                };

                return ApplicationResult<object>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer login");
                return ApplicationResult<object>.Failure("An error occurred during login");
            }
        }

        // Helper method to find user by email or phone
        private async Task<ApplicationUser?> FindUserByEmailOrPhoneAsync(string emailOrPhone, CancellationToken cancellationToken)
        {
            ApplicationUser? user;

            // Check if input is email
            if (emailOrPhone.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(emailOrPhone);
            }
            else
            {
                // Format phone number
                var formattedPhone = FormatEgyptianPhoneNumber(emailOrPhone);
                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone, cancellationToken);
            }

            return user;
        }

        // Helper method to format Egyptian phone numbers
        private string FormatEgyptianPhoneNumber(string phoneNumber)
        {
            // Remove any spaces, dashes, or special characters
            var cleaned = phoneNumber.Replace(" ", "")
                                   .Replace("-", "")
                                   .Replace("(", "")
                                   .Replace(")", "")
                                   .Replace("+", "");

            // Handle different Egyptian phone number formats
            if (cleaned.StartsWith("200")) // +200 instead of +20
            {
                cleaned = cleaned.Substring(1); // Remove the extra 0
            }

            // Add +20 prefix
            if (cleaned.StartsWith("20"))
            {
                cleaned = "+" + cleaned;
            }
            else if (cleaned.StartsWith("0"))
            {
                // Replace leading 0 with +20
                cleaned = "+20" + cleaned.Substring(1);
            }
            else if (cleaned.Length == 10 && cleaned.StartsWith("1")) // Just 10 digits starting with 1
            {
                cleaned = "+20" + cleaned;
            }
            else if (!cleaned.StartsWith("+"))
            {
                cleaned = "+20" + cleaned;
            }

            // Validate Egyptian phone number format
            // Should be +201XXXXXXXXX (13 characters total)
            if (cleaned.Length != 13 || !cleaned.StartsWith("+201"))
            {
                _logger.LogWarning($"Invalid phone number format: {phoneNumber} -> {cleaned}");
            }

            return cleaned;
        }

        // Helper method to get required actions for customers
        private List<string> GetCustomerRequiredActions(ApplicationUser user, Domain.Entities.CustomerProfile? profile)
        {
            var actions = new List<string>();

            // Check if profile exists and is complete
            if (profile == null)
            {
                actions.Add("CREATE_PROFILE");
            }
            else
            {
                // Check for incomplete profile fields
                if (string.IsNullOrWhiteSpace(profile.Name.FirstName) ||
                    string.IsNullOrWhiteSpace(profile.Name.LastName))
                {
                    actions.Add("COMPLETE_BASIC_INFO");
                }

                if (!profile.DateOfBirth.HasValue)
                {
                    actions.Add("ADD_BIRTH_DATE");
                }

                if (!profile.DistrictSystemId.HasValue ||
                    string.IsNullOrWhiteSpace(profile.Address.StreetAddress))
                {
                    actions.Add("ADD_ADDRESS");
                }
            }

            return actions;
        }

        // Helper method to get metadata for customer auth
        private AuthMetadataDto? GetCustomerAuthMetadata(ApplicationUser user, Domain.Entities.CustomerProfile? profile)
        {
            if (profile == null || GetCustomerRequiredActions(user, profile).Any())
            {
                return new AuthMetadataDto(
                    ProfileCompletionUrl: "/api/customer/profile",
                    NextStepDescription: profile == null
                        ? "Please create your profile to start booking venues"
                        : "Please complete your profile information",
                    AdditionalData: new Dictionary<string, object>
                    {
                        ["hasProfile"] = profile != null,
                        ["estimatedCompletionTime"] = "2-3 minutes"
                    }
                );
            }

            return null;
        }
    }
}