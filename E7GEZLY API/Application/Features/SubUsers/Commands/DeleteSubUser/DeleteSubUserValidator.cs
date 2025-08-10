using FluentValidation;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.DeleteSubUser
{
    /// <summary>
    /// Validator for DeleteSubUserCommand
    /// </summary>
    public class DeleteSubUserValidator : AbstractValidator<DeleteSubUserCommand>
    {
        public DeleteSubUserValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Sub-user ID is required");

            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");
        }
    }
}