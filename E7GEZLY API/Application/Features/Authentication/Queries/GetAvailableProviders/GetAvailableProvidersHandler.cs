using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Services.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetAvailableProviders
{
    /// <summary>
    /// Handler for getting available social authentication providers
    /// </summary>
    public class GetAvailableProvidersHandler : IRequestHandler<GetAvailableProvidersQuery, OperationResult<AvailableProvidersDto>>
    {
        private readonly ISocialAuthService _socialAuthService;

        public GetAvailableProvidersHandler(ISocialAuthService socialAuthService)
        {
            _socialAuthService = socialAuthService;
        }

        public async Task<OperationResult<AvailableProvidersDto>> Handle(GetAvailableProvidersQuery request, CancellationToken cancellationToken)
        {
            var isAppleDevice = IsAppleDevice(request.UserAgent);
            var providers = _socialAuthService.GetAvailableProviders(isAppleDevice);
            
            var result = new AvailableProvidersDto(providers, isAppleDevice);
            
            return await Task.FromResult(OperationResult<AvailableProvidersDto>.Success(result));
        }

        private bool IsAppleDevice(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            var appleDeviceIdentifiers = new[] { "iPhone", "iPad", "Mac", "Darwin" };
            return appleDeviceIdentifiers.Any(identifier =>
                userAgent.Contains(identifier, StringComparison.OrdinalIgnoreCase));
        }
    }
}