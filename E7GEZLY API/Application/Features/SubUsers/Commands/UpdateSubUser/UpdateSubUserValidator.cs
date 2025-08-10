using E7GEZLY_API.Models;
using FluentValidation;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.UpdateSubUser
{
    /// <summary>
    /// Validator for UpdateSubUserCommand
    /// </summary>
    public class UpdateSubUserValidator : AbstractValidator<UpdateSubUserCommand>
    {
        public UpdateSubUserValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Sub-user ID is required");

            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");

            RuleFor(x => x.Username)
                .MaximumLength(50)
                .WithMessage("Username cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Username));

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid role specified")
                .When(x => x.Role.HasValue);

            RuleFor(x => x.Permissions)
                .Must(permissions => permissions.HasValue)
                .WithMessage("Valid permissions must be specified")
                .When(x => x.Permissions.HasValue);
        }
    }
}