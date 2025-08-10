using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetLinkedAccounts
{
    /// <summary>
    /// Validator for getting linked accounts query
    /// </summary>
    public class GetLinkedAccountsValidator : AbstractValidator<GetLinkedAccountsQuery>
    {
        public GetLinkedAccountsValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");
        }
    }
}