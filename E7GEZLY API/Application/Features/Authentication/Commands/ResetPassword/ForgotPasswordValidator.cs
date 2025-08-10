using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Validator for ForgotPasswordCommand
    /// </summary>
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("Email or phone number is required")
                .Must(BeValidEmailOrPhone).WithMessage("Invalid email or phone number format");

            RuleFor(x => x.UserType)
                .IsInEnum().WithMessage("Invalid user type");
        }

        private static bool BeValidEmailOrPhone(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Check if it's an email
            if (identifier.Contains("@"))
            {
                return identifier.Contains(".") && identifier.Length > 5;
            }

            // Check if it's an Egyptian phone number
            return identifier.StartsWith("01") && identifier.Length == 11 && identifier.All(char.IsDigit);
        }
    }
}