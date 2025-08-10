using FluentValidation;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteCourtProfile
{
    /// <summary>
    /// Validator for CompleteCourtProfileCommand
    /// </summary>
    public class CompleteCourtProfileValidator : AbstractValidator<CompleteCourtProfileCommand>
    {
        public CompleteCourtProfileValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.DistrictId)
                .GreaterThan(0)
                .WithMessage("District ID must be valid");

            RuleFor(x => x.StreetAddress)
                .MaximumLength(500)
                .WithMessage("Street address cannot exceed 500 characters");

            RuleFor(x => x.Landmark)
                .MaximumLength(200)
                .WithMessage("Landmark cannot exceed 200 characters");

            RuleFor(x => x.WorkingHours)
                .NotEmpty()
                .WithMessage("Working hours are required")
                .Must(wh => wh.Count == 7)
                .WithMessage("Working hours must be specified for all 7 days");

            RuleFor(x => x.MorningHourPrice)
                .GreaterThan(0)
                .WithMessage("Morning hour price must be greater than 0")
                .LessThan(999999.99m)
                .WithMessage("Morning hour price cannot exceed 999,999.99");

            RuleFor(x => x.EveningHourPrice)
                .GreaterThan(0)
                .WithMessage("Evening hour price must be greater than 0")
                .LessThan(999999.99m)
                .WithMessage("Evening hour price cannot exceed 999,999.99");

            RuleFor(x => x.DepositPercentage)
                .InclusiveBetween(0, 100)
                .WithMessage("Deposit percentage must be between 0 and 100");

            RuleFor(x => x.MorningEndTime)
                .GreaterThan(x => x.MorningStartTime)
                .WithMessage("Morning end time must be after morning start time");

            RuleFor(x => x.EveningEndTime)
                .GreaterThan(x => x.EveningStartTime)
                .WithMessage("Evening end time must be after evening start time");

            RuleForEach(x => x.WorkingHours).ChildRules(wh =>
            {
                wh.RuleFor(x => x.DayOfWeek)
                    .IsInEnum()
                    .WithMessage("Invalid day of week");

                wh.When(x => !x.IsClosed, () =>
                {
                    wh.RuleFor(x => x.OpenTime)
                        .NotNull()
                        .WithMessage("Open time is required for non-closed days");

                    wh.RuleFor(x => x.CloseTime)
                        .NotNull()
                        .WithMessage("Close time is required for non-closed days")
                        .GreaterThan(x => x.OpenTime)
                        .WithMessage("Close time must be after open time");
                });
            });
        }
    }
}