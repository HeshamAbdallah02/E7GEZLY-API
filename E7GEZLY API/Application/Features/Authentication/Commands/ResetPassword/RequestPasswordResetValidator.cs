using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Validator for RequestPasswordResetCommand
    /// </summary>
    public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetValidator()
        {
            RuleFor(x => x.EmailOrPhone)
                .NotEmpty()
                .WithMessage("Email or phone number is required")
                .Must(BeValidEmailOrPhone)
                .WithMessage("Invalid email or phone number format");

            RuleFor(x => x.ResetMethod)
                .NotEmpty()
                .WithMessage("Reset method is required")
                .Must(x => x.Equals("Phone", StringComparison.OrdinalIgnoreCase) || 
                          x.Equals("Email", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Reset method must be 'Phone' or 'Email'");
        }

        private static bool BeValidEmailOrPhone(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            // Check if it's an email
            if (value.Contains("@"))
            {
                return new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                    .IsValid(value);
            }

            // Check if it's a phone number (Egyptian format)
            var phonePattern = @"^(\+20|20|0)?1[0-2,5]\d{8}$";
            return System.Text.RegularExpressions.Regex.IsMatch(value, phonePattern);
        }
    }
}