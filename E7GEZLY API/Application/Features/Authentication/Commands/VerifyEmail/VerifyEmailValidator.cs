using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.VerifyEmail
{
    /// <summary>
    /// Validator for VerifyEmailCommand
    /// </summary>
    public class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.VerificationCode)
                .NotEmpty()
                .WithMessage("Verification code is required")
                .Length(6, 6)
                .WithMessage("Verification code must be 6 digits")
                .Matches(@"^\d{6}$")
                .WithMessage("Verification code must contain only numbers");
        }
    }
}