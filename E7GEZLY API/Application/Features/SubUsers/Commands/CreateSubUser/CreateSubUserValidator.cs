using FluentValidation;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.CreateSubUser
{
    /// <summary>
    /// Validator for CreateSubUserCommand
    /// </summary>
    public class CreateSubUserValidator : AbstractValidator<CreateSubUserCommand>
    {
        public CreateSubUserValidator()
        {
            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required")
                .Length(2, 100)
                .WithMessage("Full name must be between 2 and 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required")
                .Matches(@"^(10|11|12|15)[0-9]{8}$")
                .WithMessage("Please enter a valid Egyptian mobile number (11 digits starting with 10, 11, 12, or 15)");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
                .WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid role specified");

            RuleFor(x => x.Permissions)
                .IsInEnum()
                .WithMessage("Invalid permissions specified");
        }
    }
}