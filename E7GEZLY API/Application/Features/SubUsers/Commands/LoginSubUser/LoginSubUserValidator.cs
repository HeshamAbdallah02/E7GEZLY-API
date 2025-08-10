using FluentValidation;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.LoginSubUser
{
    /// <summary>
    /// Validator for LoginSubUserCommand
    /// </summary>
    public class LoginSubUserValidator : AbstractValidator<LoginSubUserCommand>
    {
        public LoginSubUserValidator()
        {
            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required")
                .MinimumLength(3)
                .WithMessage("Username must be at least 3 characters long")
                .MaximumLength(50)
                .WithMessage("Username cannot exceed 50 characters");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long");
        }
    }
}