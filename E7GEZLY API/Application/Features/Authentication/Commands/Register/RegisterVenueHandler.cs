using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.Register
{
    /// <summary>
    /// Handler for RegisterVenueCommand
    /// </summary>
    public class RegisterVenueHandler : IRequestHandler<RegisterVenueCommand, ApplicationResult<VenueRegistrationResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly IVerificationService _verificationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<RegisterVenueHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public RegisterVenueHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            IVerificationService verificationService,
            IDateTimeService dateTimeService,
            ILogger<RegisterVenueHandler> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _verificationService = verificationService;
            _dateTimeService = dateTimeService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ApplicationResult<VenueRegistrationResponseDto>> Handle(RegisterVenueCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await ((AppDbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return ApplicationResult<VenueRegistrationResponseDto>.Failure("Email already registered");
                }

                // Check if phone number already exists
                var formattedPhoneNumber = $"+2{request.PhoneNumber}";
                var existingPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhoneNumber, cancellationToken);
                if (existingPhone != null)
                {
                    return ApplicationResult<VenueRegistrationResponseDto>.Failure("Phone number already registered");
                }

                // Create venue with basic info only (no location yet)  
                var venue = Domain.Entities.Venue.Create(
                    request.VenueName,
                    request.VenueType,
                    request.Email
                );

                _context.Venues.Add(venue);

                // Create user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    PhoneNumber = formattedPhoneNumber,
                    VenueId = venue.Id,
                    CreatedAt = _dateTimeService.UtcNow,
                    IsActive = true,
                    IsPhoneNumberVerified = false,
                    IsEmailVerified = false
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ApplicationResult<VenueRegistrationResponseDto>.Failure(
                        result.Errors.Select(e => e.Description).ToArray());
                }

                // Assign role
                try
                {
                    await _userManager.AddToRoleAsync(user, DbInitializer.AppRoles.VenueAdmin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning role to venue user");
                    await transaction.RollbackAsync(cancellationToken);
                    return ApplicationResult<VenueRegistrationResponseDto>.Failure("Error assigning user role");
                }

                // Save to generate IDs
                await _context.SaveChangesAsync(cancellationToken);

                // Generate and send phone verification code
                string? verificationCode = null;
                try
                {
                    var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                    if (success)
                    {
                        user.PhoneNumberVerificationCode = code;
                        user.PhoneNumberVerificationCodeExpiry = _dateTimeService.UtcNow.AddMinutes(10);
                        await _userManager.UpdateAsync(user);

                        await _verificationService.SendPhoneVerificationAsync(request.PhoneNumber, code);
                        verificationCode = code;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating/sending verification code");
                }

                await transaction.CommitAsync(cancellationToken);

                // Send welcome email (non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _verificationService.SendWelcomeEmailAsync(
                            user.Email!,
                            venue.Name.Value,
                            "Venue"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email");
                    }
                }, cancellationToken);

                _logger.LogInformation($"New venue registered: {venue.Name} ({request.VenueType})");

                // Build response
                var response = new VenueRegistrationResponseDto
                {
                    Success = true,
                    Message = "Registration successful. Please verify your phone number.",
                    UserId = user.Id,
                    VenueId = venue.Id,
                    RequiresVerification = true,
                    RequiresProfileCompletion = true,
                    VerificationCode = _environment.IsDevelopment() ? verificationCode : null
                };

                return ApplicationResult<VenueRegistrationResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during venue registration");
                await transaction.RollbackAsync(cancellationToken);
                return ApplicationResult<VenueRegistrationResponseDto>.Failure("An error occurred during registration");
            }
        }
    }
}