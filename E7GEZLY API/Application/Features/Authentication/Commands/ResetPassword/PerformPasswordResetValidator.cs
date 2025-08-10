using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Validator for PerformPasswordResetCommand
    /// </summary>
    public class PerformPasswordResetValidator : AbstractValidator<PerformPasswordResetCommand>
    {
        public PerformPasswordResetValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.ResetCode)
                .NotEmpty().WithMessage("Reset code is required")
                .Length(6).WithMessage("Reset code must be 6 digits");

            RuleFor(x => x.Method)
                .IsInEnum().WithMessage("Invalid reset method");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }
}