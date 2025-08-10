using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Validator for ValidateResetCodeCommand
    /// </summary>
    public class ValidateResetCodeValidator : AbstractValidator<ValidateResetCodeCommand>
    {
        public ValidateResetCodeValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.ResetCode)
                .NotEmpty().WithMessage("Reset code is required");

            RuleFor(x => x.Method)
                .IsInEnum().WithMessage("Invalid reset method");
        }
    }
}