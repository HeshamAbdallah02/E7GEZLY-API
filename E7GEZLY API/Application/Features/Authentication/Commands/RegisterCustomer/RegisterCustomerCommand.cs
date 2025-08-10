using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.RegisterCustomer
{
    /// <summary>
    /// Command for registering a new customer
    /// </summary>
    public class RegisterCustomerCommand : IRequest<ApplicationResult<RegistrationResponseDto>>
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public DateTime DateOfBirth { get; init; }
        
        // Address information
        public string? Governorate { get; init; }
        public string? District { get; init; }
        public string? StreetAddress { get; init; }
        public string? Landmark { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
    }
}