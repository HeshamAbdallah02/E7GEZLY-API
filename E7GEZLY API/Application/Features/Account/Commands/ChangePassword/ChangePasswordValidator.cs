using FluentValidation;

namespace E7GEZLY_API.Application.Features.Account.Commands.ChangePassword
{
    /// <summary>
    /// Validator for ChangePasswordCommand
    /// </summary>
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Current password is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long");
        }
    }
}