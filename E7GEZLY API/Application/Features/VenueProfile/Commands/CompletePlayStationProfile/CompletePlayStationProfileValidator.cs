using FluentValidation;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompletePlayStationProfile
{
    /// <summary>
    /// Validator for CompletePlayStationProfileCommand
    /// </summary>
    public class CompletePlayStationProfileValidator : AbstractValidator<CompletePlayStationProfileCommand>
    {
        public CompletePlayStationProfileValidator()
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

            RuleFor(x => x.NumberOfRooms)
                .InclusiveBetween(1, 1000)
                .WithMessage("Number of rooms must be between 1 and 1000");

            RuleFor(x => x)
                .Must(x => x.HasPS4 || x.HasPS5)
                .WithMessage("Venue must have at least PS4 or PS5");

            // PlayStation pricing validation
            When(x => x.HasPS4, () =>
            {
                RuleFor(x => x.PS4Pricing)
                    .NotNull()
                    .WithMessage("PS4 pricing is required when PS4 is available");
            });

            When(x => x.HasPS5, () =>
            {
                RuleFor(x => x.PS5Pricing)
                    .NotNull()
                    .WithMessage("PS5 pricing is required when PS5 is available");
            });

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