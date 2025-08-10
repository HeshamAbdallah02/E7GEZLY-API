using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SocialLogin
{
    /// <summary>
    /// Validator for social login command
    /// </summary>
    public class SocialLoginValidator : AbstractValidator<SocialLoginCommand>
    {
        public SocialLoginValidator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage("Provider is required")
                .Must(BeValidProvider)
                .WithMessage("Provider must be one of: google, facebook, apple");

            RuleFor(x => x.AccessToken)
                .NotEmpty()
                .WithMessage("Access token is required")
                .MinimumLength(10)
                .WithMessage("Access token appears to be invalid");

            RuleFor(x => x.DeviceName)
                .MaximumLength(100)
                .WithMessage("Device name must not exceed 100 characters");

            RuleFor(x => x.DeviceType)
                .MaximumLength(50)
                .WithMessage("Device type must not exceed 50 characters");
        }

        private bool BeValidProvider(string provider)
        {
            var validProviders = new[] { "google", "facebook", "apple" };
            return validProviders.Contains(provider?.ToLower());
        }
    }
}