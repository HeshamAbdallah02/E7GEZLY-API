using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetAvailableProviders
{
    /// <summary>
    /// Validator for getting available providers query
    /// </summary>
    public class GetAvailableProvidersValidator : AbstractValidator<GetAvailableProvidersQuery>
    {
        public GetAvailableProvidersValidator()
        {
            // No validation rules needed for this query
            // UserAgent is optional and can be any string
        }
    }
}