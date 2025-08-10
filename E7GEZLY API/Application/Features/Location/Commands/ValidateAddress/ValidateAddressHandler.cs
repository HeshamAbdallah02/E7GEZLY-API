using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.Services.Location;
using MediatR;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Location.Commands.ValidateAddress
{
    /// <summary>
    /// Handler for validating an address
    /// </summary>
    public class ValidateAddressHandler : IRequestHandler<ValidateAddressCommand, ApplicationResult<AddressValidationResultDto>>
    {
        private readonly ILocationService _locationService;
        private readonly ILogger<ValidateAddressHandler> _logger;

        public ValidateAddressHandler(
            ILocationService locationService,
            ILogger<ValidateAddressHandler> logger)
        {
            _locationService = locationService;
            _logger = logger;
        }

        public async Task<ApplicationResult<AddressValidationResultDto>> Handle(
            ValidateAddressCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Validating address for district {DistrictId}", request.AddressDto.DistrictId);

                var result = await _locationService.ValidateAddressAsync(request.AddressDto);

                _logger.LogInformation("Address validation completed successfully");

                return ApplicationResult<AddressValidationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating address");
                return ApplicationResult<AddressValidationResultDto>.Failure("Failed to validate address");
            }
        }
    }
}