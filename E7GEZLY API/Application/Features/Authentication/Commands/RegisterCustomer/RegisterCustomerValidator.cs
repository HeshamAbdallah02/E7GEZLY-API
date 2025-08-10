using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.RegisterCustomer
{
    /// <summary>
    /// Validator for RegisterCustomerCommand
    /// </summary>
    public class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
    {
        public RegisterCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .Length(2, 50).WithMessage("First name must be between 2 and 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^01[0125]\d{8}$").WithMessage("Phone number must be in format 01xxxxxxxxx (11 digits starting with 010, 011, 012, or 015)");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");

            RuleFor(x => x.DateOfBirth)
                .Must(BeValidAge).WithMessage("You must be at least 15 years old to register")
                .Must(NotBeTooOld).WithMessage("Please enter a valid date of birth");
        }

        private static bool BeValidAge(DateTime dateOfBirth)
        {
            var age = DateTime.Today.Year - dateOfBirth.Year;
            if (DateTime.Today.DayOfYear < dateOfBirth.DayOfYear) age--;
            return age >= 15;
        }

        private static bool NotBeTooOld(DateTime dateOfBirth)
        {
            var age = DateTime.Today.Year - dateOfBirth.Year;
            if (DateTime.Today.DayOfYear < dateOfBirth.DayOfYear) age--;
            return age <= 80;
        }
    }
}