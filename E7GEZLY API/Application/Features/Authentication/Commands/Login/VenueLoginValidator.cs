using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.Login
{
    /// <summary>
    /// Validator for VenueLoginCommand
    /// </summary>
    public class VenueLoginValidator : AbstractValidator<VenueLoginCommand>
    {
        public VenueLoginValidator()
        {
            RuleFor(x => x.EmailOrPhone)
                .NotEmpty()
                .WithMessage("Email or phone number is required");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required");

            When(x => x.EmailOrPhone.Contains('@'), () => 
            {
                RuleFor(x => x.EmailOrPhone)
                    .EmailAddress()
                    .WithMessage("Invalid email format");
            });

            When(x => !x.EmailOrPhone.Contains('@'), () => 
            {
                RuleFor(x => x.EmailOrPhone)
                    .Matches(@"^((\+20)?(10|11|12|15)[0-9]{8})$")
                    .WithMessage("Please enter a valid Egyptian mobile number");
            });
        }
    }
}