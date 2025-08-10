using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.CustomerLogin
{
    /// <summary>
    /// Validator for CustomerLoginCommand
    /// </summary>
    public class CustomerLoginValidator : AbstractValidator<CustomerLoginCommand>
    {
        public CustomerLoginValidator()
        {
            RuleFor(x => x.EmailOrPhone)
                .NotEmpty().WithMessage("Email or phone number is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}