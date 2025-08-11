using FluentValidation;

namespace E7GEZLY_API.Application.Features.Account.Commands.DeactivateAccount
{
    /// <summary>
    /// Validator for DeactivateAccountCommand
    /// </summary>
    public class DeactivateAccountValidator : AbstractValidator<DeactivateAccountCommand>
    {
        public DeactivateAccountValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required for account deactivation");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Reason));
        }
    }
}