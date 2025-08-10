using FluentValidation;

namespace E7GEZLY_API.Application.Features.VenueProfile.Queries.IsProfileComplete
{
    /// <summary>
    /// Validator for IsProfileCompleteQuery
    /// </summary>
    public class IsProfileCompleteValidator : AbstractValidator<IsProfileCompleteQuery>
    {
        public IsProfileCompleteValidator()
        {
            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");
        }
    }
}