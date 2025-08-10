using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.UnlinkSocialAccount
{
    /// <summary>
    /// Validator for unlinking social account command
    /// </summary>
    public class UnlinkSocialAccountValidator : AbstractValidator<UnlinkSocialAccountCommand>
    {
        public UnlinkSocialAccountValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage("Provider is required")
                .Must(BeValidProvider)
                .WithMessage("Provider must be one of: google, facebook, apple");
        }

        private bool BeValidProvider(string provider)
        {
            var validProviders = new[] { "google", "facebook", "apple" };
            return validProviders.Contains(provider?.ToLower());
        }
    }
}