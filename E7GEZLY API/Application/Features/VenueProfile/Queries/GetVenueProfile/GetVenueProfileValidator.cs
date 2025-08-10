using FluentValidation;

namespace E7GEZLY_API.Application.Features.VenueProfile.Queries.GetVenueProfile
{
    /// <summary>
    /// Validator for GetVenueProfileQuery
    /// </summary>
    public class GetVenueProfileValidator : AbstractValidator<GetVenueProfileQuery>
    {
        public GetVenueProfileValidator()
        {
            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");
        }
    }
}