using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.RegisterCustomer
{
    /// <summary>
    /// Handler for RegisterCustomerCommand
    /// </summary>
    public class RegisterCustomerHandler : IRequestHandler<RegisterCustomerCommand, ApplicationResult<RegistrationResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly IVerificationService _verificationService;
        private readonly ILogger<RegisterCustomerHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public RegisterCustomerHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            IVerificationService verificationService,
            ILogger<RegisterCustomerHandler> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _verificationService = verificationService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ApplicationResult<RegistrationResponseDto>> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await ((AppDbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return ApplicationResult<RegistrationResponseDto>.Failure("Email already registered");
                }

                // Check if phone number already exists
                var formattedPhoneNumber = $"+2{request.PhoneNumber}";
                var existingPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhoneNumber, cancellationToken);
                if (existingPhone != null)
                {
                    return ApplicationResult<RegistrationResponseDto>.Failure("Phone number already registered");
                }

                // Create user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    PhoneNumber = formattedPhoneNumber,
                    VenueId = null,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsPhoneNumberVerified = false,
                    IsEmailVerified = false
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ApplicationResult<RegistrationResponseDto>.Failure(
                        result.Errors.Select(e => e.Description).ToArray());
                }

                // Assign role
                try
                {
                    await _userManager.AddToRoleAsync(user, DbInitializer.AppRoles.Customer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning role to user");
                    await transaction.RollbackAsync(cancellationToken);
                    return ApplicationResult<RegistrationResponseDto>.Failure("Error assigning user role");
                }

                // Find the district by name if provided
                int? districtId = null;
                if (!string.IsNullOrWhiteSpace(request.Governorate) && !string.IsNullOrWhiteSpace(request.District))
                {
                    var district = await _context.Districts
                        .Include(d => d.Governorate)
                        .FirstOrDefaultAsync(d =>
                            (d.NameEn.ToLower() == request.District.ToLower() ||
                             d.NameAr == request.District) &&
                            (d.Governorate.NameEn.ToLower() == request.Governorate.ToLower() ||
                             d.Governorate.NameAr == request.Governorate), cancellationToken);

                    if (district != null)
                    {
                        districtId = district.Id;
                    }
                }

                // Create customer profile using factory method
                var profile = Domain.Entities.CustomerProfile.Create(
                    user.Id,
                    request.FirstName,
                    request.LastName,
                    request.DateOfBirth,
                    request.StreetAddress,
                    request.Landmark,
                    request.Latitude,
                    request.Longitude,
                    districtId
                );

                try
                {
                    _context.CustomerProfiles.Add(profile);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer profile");
                    await transaction.RollbackAsync(cancellationToken);
                    return ApplicationResult<RegistrationResponseDto>.Failure("Error creating customer profile");
                }

                // Generate and send phone verification code
                string? verificationCode = null;
                try
                {
                    var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                    if (success)
                    {
                        user.PhoneNumberVerificationCode = code;
                        user.PhoneNumberVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                        await _userManager.UpdateAsync(user);

                        await _verificationService.SendPhoneVerificationAsync(request.PhoneNumber, code);
                        verificationCode = code;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating/sending verification code");
                    // Don't fail the registration if verification fails
                }

                await transaction.CommitAsync(cancellationToken);

                // Send welcome email (non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _verificationService.SendWelcomeEmailAsync(
                            user.Email!,
                            profile.Name.FirstName,
                            "Customer"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email");
                    }
                }, cancellationToken);

                _logger.LogInformation($"New customer registered: {user.Email}");

                // Build response
                var response = new RegistrationResponseDto(
                    Success: true,
                    Message: "Registration successful. Please verify your phone number.",
                    UserId: user.Id,
                    RequiresVerification: true
                );

                return ApplicationResult<RegistrationResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer registration");
                await transaction.RollbackAsync(cancellationToken);
                return ApplicationResult<RegistrationResponseDto>.Failure("An error occurred during registration");
            }
        }
    }
}