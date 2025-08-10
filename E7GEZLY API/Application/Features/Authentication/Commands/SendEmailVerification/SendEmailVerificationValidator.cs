using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SendEmailVerification
{
    /// <summary>
    /// Validator for SendEmailVerificationCommand
    /// </summary>
    public class SendEmailVerificationValidator : AbstractValidator<SendEmailVerificationCommand>
    {
        public SendEmailVerificationValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");
        }
    }
}