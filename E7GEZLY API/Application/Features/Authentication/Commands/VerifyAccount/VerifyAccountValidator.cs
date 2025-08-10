using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.VerifyAccount
{
    /// <summary>
    /// Validator for VerifyAccountCommand
    /// </summary>
    public class VerifyAccountValidator : AbstractValidator<VerifyAccountCommand>
    {
        public VerifyAccountValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.VerificationCode)
                .NotEmpty()
                .WithMessage("Verification code is required")
                .Length(6)
                .WithMessage("Verification code must be 6 digits");

            RuleFor(x => x.Method)
                .IsInEnum()
                .WithMessage("Valid verification method is required");
        }
    }
}