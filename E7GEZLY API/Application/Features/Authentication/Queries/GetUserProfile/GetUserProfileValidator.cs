using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetUserProfile
{
    /// <summary>
    /// Validator for GetUserProfileQuery
    /// </summary>
    public class GetUserProfileValidator : AbstractValidator<GetUserProfileQuery>
    {
        public GetUserProfileValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required")
                .Length(36)
                .WithMessage("Invalid User ID format");
        }
    }
}