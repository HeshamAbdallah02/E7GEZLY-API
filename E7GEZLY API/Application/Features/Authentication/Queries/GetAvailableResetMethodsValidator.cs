using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Queries
{
    /// <summary>
    /// Validator for GetAvailableResetMethodsQuery
    /// </summary>
    public class GetAvailableResetMethodsValidator : AbstractValidator<GetAvailableResetMethodsQuery>
    {
        public GetAvailableResetMethodsValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}