using FluentValidation;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUsers
{
    /// <summary>
    /// Validator for GetSubUsersQuery
    /// </summary>
    public class GetSubUsersValidator : AbstractValidator<GetSubUsersQuery>
    {
        public GetSubUsersValidator()
        {
            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");
        }
    }
}