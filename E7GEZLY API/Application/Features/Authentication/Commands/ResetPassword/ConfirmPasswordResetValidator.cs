using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Validator for ConfirmPasswordResetCommand
    /// </summary>
    public class ConfirmPasswordResetValidator : AbstractValidator<ConfirmPasswordResetCommand>
    {
        public ConfirmPasswordResetValidator()
        {
            RuleFor(x => x.EmailOrPhone)
                .NotEmpty()
                .WithMessage("Email or phone number is required")
                .Must(BeValidEmailOrPhone)
                .WithMessage("Invalid email or phone number format");

            RuleFor(x => x.ResetCode)
                .NotEmpty()
                .WithMessage("Reset code is required")
                .Length(6, 6)
                .WithMessage("Reset code must be 6 digits");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]")
                .WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"\d")
                .WithMessage("Password must contain at least one digit")
                .Matches(@"[\!\?\*\.\@\#\$\%\^\&\(\)\-\+\=]")
                .WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password confirmation is required")
                .Equal(x => x.NewPassword)
                .WithMessage("Password confirmation does not match");
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