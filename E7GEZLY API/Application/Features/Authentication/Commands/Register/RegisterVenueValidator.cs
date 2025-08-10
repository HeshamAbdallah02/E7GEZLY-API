using E7GEZLY_API.Models;
using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.Register
{
    /// <summary>
    /// Validator for RegisterVenueCommand
    /// </summary>
    public class RegisterVenueValidator : AbstractValidator<RegisterVenueCommand>
    {
        public RegisterVenueValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email format is invalid");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
                .WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required")
                .Matches(@"^(10|11|12|15)[0-9]{8}$")
                .WithMessage("Please enter a valid Egyptian mobile number (11 digits starting with 10, 11, 12, or 15)");

            RuleFor(x => x.VenueName)
                .NotEmpty()
                .WithMessage("Venue name is required")
                .Length(2, 100)
                .WithMessage("Venue name must be between 2 and 100 characters");

            RuleFor(x => x.VenueType)
                .IsInEnum()
                .WithMessage("Please select a valid venue type");
        }
    }
}