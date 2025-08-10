using FluentValidation;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SendVerificationCode
{
    /// <summary>
    /// Validator for SendVerificationCodeCommand
    /// </summary>
    public class SendVerificationCodeValidator : AbstractValidator<SendVerificationCodeCommand>
    {
        public SendVerificationCodeValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.Method)
                .IsInEnum()
                .WithMessage("Valid verification method is required");

            RuleFor(x => x.Purpose)
                .IsInEnum()
                .WithMessage("Valid verification purpose is required");
        }
    }
}