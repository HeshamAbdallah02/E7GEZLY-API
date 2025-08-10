using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using MediatR;

namespace E7GEZLY_API.Application.Features.Location.Commands.ValidateAddress
{
    /// <summary>
    /// Command to validate an address
    /// </summary>
    public record ValidateAddressCommand(ValidateAddressDto AddressDto) : IRequest<ApplicationResult<AddressValidationResultDto>>;
}