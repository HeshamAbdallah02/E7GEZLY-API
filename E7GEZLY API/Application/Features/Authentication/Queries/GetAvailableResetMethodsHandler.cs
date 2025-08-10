using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Application.Features.Authentication.Queries
{
    /// <summary>
    /// Handler for GetAvailableResetMethodsQuery
    /// </summary>
    public class GetAvailableResetMethodsHandler : IRequestHandler<GetAvailableResetMethodsQuery, ApplicationResult<AvailableResetMethodsResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GetAvailableResetMethodsHandler> _logger;

        public GetAvailableResetMethodsHandler(
            UserManager<ApplicationUser> userManager,
            ILogger<GetAvailableResetMethodsHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ApplicationResult<AvailableResetMethodsResponseDto>> Handle(GetAvailableResetMethodsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<AvailableResetMethodsResponseDto>.Failure("User not found");
                }

                var availableMethods = new List<ResetMethodInfo>();

                if (!string.IsNullOrEmpty(user.PhoneNumber) && user.IsPhoneNumberVerified)
                {
                    availableMethods.Add(new ResetMethodInfo
                    {
                        Method = "Phone",
                        Value = (int)ResetMethod.Phone,
                        MaskedValue = MaskPhoneNumber(user.PhoneNumber)
                    });
                }

                if (!string.IsNullOrEmpty(user.Email) && user.IsEmailVerified)
                {
                    availableMethods.Add(new ResetMethodInfo
                    {
                        Method = "Email",
                        Value = (int)ResetMethod.Email,
                        MaskedValue = MaskEmail(user.Email)
                    });
                }

                if (!availableMethods.Any())
                {
                    return ApplicationResult<AvailableResetMethodsResponseDto>.Failure(
                        "No verified contact methods available. Please contact support.");
                }

                return ApplicationResult<AvailableResetMethodsResponseDto>.Success(new AvailableResetMethodsResponseDto
                {
                    Success = true,
                    AvailableMethods = availableMethods,
                    PreferredMethod = availableMethods.First()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailableResetMethodsHandler");
                return ApplicationResult<AvailableResetMethodsResponseDto>.Failure("An error occurred checking reset methods");
            }
        }

        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 6)
                return "****";

            // Remove country code if present
            var localNumber = phoneNumber.StartsWith("+2") ? phoneNumber.Substring(2) : phoneNumber;

            // Show first 3 and last 2 digits
            if (localNumber.Length >= 11)
            {
                return $"{localNumber.Substring(0, 3)}****{localNumber.Substring(localNumber.Length - 2)}";
            }

            return "****";
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return "****";

            var parts = email.Split('@');
            var localPart = parts[0];
            var domain = parts[1];

            if (localPart.Length <= 2)
            {
                return $"{localPart}****@{domain}";
            }

            return $"{localPart.Substring(0, 2)}****@{domain}";
        }
    }
}