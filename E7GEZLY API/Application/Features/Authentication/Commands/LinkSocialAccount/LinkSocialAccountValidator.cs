using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.LinkSocialAccount
{
    /// <summary>
    /// Validator for linking social account command
    /// </summary>
    public class LinkSocialAccountValidator : AbstractValidator<LinkSocialAccountCommand>
    {
        public LinkSocialAccountValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

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
        }

        private bool BeValidProvider(string provider)
        {
            var validProviders = new[] { "google", "facebook", "apple" };
            return validProviders.Contains(provider?.ToLower());
        }
    }
}