using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using MediatR;

namespace E7GEZLY_API.Application.Features.Location.Commands.GeocodeCoordinates
{
    /// <summary>
    /// Command to geocode coordinates to address
    /// </summary>
    public record GeocodeCoordinatesCommand(double Latitude, double Longitude) : IRequest<ApplicationResult<GeocodeResponseDto>>;
}