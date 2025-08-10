using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.ValidateToken
{
    /// <summary>
    /// Validator for ValidateTokenQuery
    /// </summary>
    public class ValidateTokenValidator : AbstractValidator<ValidateTokenQuery>
    {
        public ValidateTokenValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Token is required")
                .Must(BeValidJwtFormat)
                .WithMessage("Invalid JWT token format");
        }

        private static bool BeValidJwtFormat(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            // Basic JWT format validation (3 parts separated by dots)
            var parts = token.Split('.');
            return parts.Length == 3 && 
                   parts.All(part => !string.IsNullOrEmpty(part) && 
                                   System.Text.RegularExpressions.Regex.IsMatch(part, @"^[A-Za-z0-9_-]+$"));
        }
    }
}