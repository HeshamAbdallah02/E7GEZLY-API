using FluentValidation;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteProfile
{
    /// <summary>
    /// Validator for CompleteVenueProfileCommand
    /// </summary>
    public class CompleteVenueProfileValidator : AbstractValidator<CompleteVenueProfileCommand>
    {
        public CompleteVenueProfileValidator()
        {
            RuleFor(x => x.VenueId)
                .NotEmpty()
                .WithMessage("Venue ID is required");

            RuleFor(x => x.StreetAddress)
                .NotEmpty()
                .WithMessage("Street address is required")
                .MaximumLength(500)
                .WithMessage("Street address must not exceed 500 characters");

            RuleFor(x => x.Landmark)
                .MaximumLength(200)
                .WithMessage("Landmark must not exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Landmark));

            RuleFor(x => x.DistrictId)
                .NotEmpty()
                .WithMessage("District is required");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(22.0, 32.0)
                .WithMessage("Latitude must be within Egypt's boundaries (22.0 to 32.0)");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(25.0, 37.0)
                .WithMessage("Longitude must be within Egypt's boundaries (25.0 to 37.0)");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.WorkingHours)
                .NotEmpty()
                .WithMessage("Working hours are required")
                .Must(HaveAtLeastOneWorkingDay)
                .WithMessage("At least one working day is required");

            RuleForEach(x => x.WorkingHours).SetValidator(new VenueWorkingHoursValidator());

            RuleFor(x => x.Pricing)
                .NotEmpty()
                .WithMessage("Pricing information is required");

            RuleForEach(x => x.Pricing).SetValidator(new VenuePricingValidator());

            RuleFor(x => x.ImageUrls)
                .Must(x => x.Count <= 10)
                .WithMessage("Maximum 10 images allowed");

            RuleForEach(x => x.ImageUrls)
                .NotEmpty()
                .WithMessage("Image URL cannot be empty")
                .Must(BeAValidUrl)
                .WithMessage("Invalid image URL format");

            // PlayStation-specific validation
            When(x => x.PlayStationDetails != null, () => {
                RuleFor(x => x.PlayStationDetails!)
                    .SetValidator(new VenuePlayStationDetailsValidator());
            });
        }

        private bool HaveAtLeastOneWorkingDay(List<DTOs.Venue.VenueWorkingHoursDto> workingHours)
        {
            return workingHours.Any(wh => wh.IsWorkingDay());
        }

        private bool BeAValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    public class VenueWorkingHoursValidator : AbstractValidator<DTOs.Venue.VenueWorkingHoursDto>
    {
        public VenueWorkingHoursValidator()
        {
            RuleFor(x => x.DayOfWeek)
                .IsInEnum()
                .WithMessage("Day of week must be a valid DayOfWeek value");

            When(x => x.IsWorkingDay(), () => {
                RuleFor(x => x.OpenTime)
                    .NotEmpty()
                    .WithMessage("Open time is required for working days");

                RuleFor(x => x.CloseTime)
                    .NotEmpty()
                    .WithMessage("Close time is required for working days")
                    .GreaterThan(x => x.OpenTime)
                    .WithMessage("Close time must be after open time");
            });
        }
    }

    public class VenuePricingValidator : AbstractValidator<DTOs.Venue.VenuePricingDto>
    {
        public VenuePricingValidator()
        {
            RuleFor(x => x.Type)
                .NotNull()
                .WithMessage("Pricing type is required")
                .IsInEnum()
                .WithMessage("Invalid pricing type");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0")
                .LessThan(10000)
                .WithMessage("Price must be less than 10,000 EGP");

            RuleFor(x => x.DepositPercentage)
                .InclusiveBetween(0, 100)
                .WithMessage("Deposit percentage must be between 0 and 100")
                .When(x => x.DepositPercentage.HasValue);
        }
    }

    public class VenuePlayStationDetailsValidator : AbstractValidator<DTOs.Venue.VenuePlayStationDetailsDto>
    {
        public VenuePlayStationDetailsValidator()
        {
            RuleFor(x => x.NumberOfConsoles)
                .GreaterThan(0)
                .WithMessage("Number of consoles must be greater than 0")
                .LessThanOrEqualTo(50)
                .WithMessage("Number of consoles must be 50 or less");

            RuleFor(x => x.ConsoleTypes)
                .NotEmpty()
                .WithMessage("Console types are required")
                .MaximumLength(200)
                .WithMessage("Console types must not exceed 200 characters");

            When(x => x.HasPrivateRooms, () => {
                RuleFor(x => x.NumberOfPrivateRooms)
                    .GreaterThan(0)
                    .WithMessage("Number of private rooms must be greater than 0 when private rooms are available")
                    .LessThanOrEqualTo(20)
                    .WithMessage("Number of private rooms must be 20 or less");
            });
        }
    }
}