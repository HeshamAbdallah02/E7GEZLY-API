using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.RefreshToken
{
    /// <summary>
    /// Validator for RefreshTokenCommand
    /// </summary>
    public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("Refresh token is required")
                .MinimumLength(32)
                .WithMessage("Invalid refresh token format");

            RuleFor(x => x.DeviceName)
                .MaximumLength(100)
                .WithMessage("Device name cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.DeviceName));

            RuleFor(x => x.DeviceType)
                .MaximumLength(50)
                .WithMessage("Device type cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.DeviceType));

            RuleFor(x => x.UserAgent)
                .MaximumLength(500)
                .WithMessage("User agent cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.UserAgent));

            RuleFor(x => x.IpAddress)
                .MaximumLength(45)
                .WithMessage("IP address cannot exceed 45 characters")
                .When(x => !string.IsNullOrEmpty(x.IpAddress));
        }
    }
}