using FluentValidation;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUser
{
    /// <summary>
    /// Validator for GetSubUserQuery
    /// </summary>
    public class GetSubUserValidator : AbstractValidator<GetSubUserQuery>
    {
        public GetSubUserValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Sub-user ID is required");

            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");
        }
    }
}