using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.CreateFirstAdmin
{
    /// <summary>
    /// Handler for CreateFirstAdminCommand
    /// </summary>
    public class CreateFirstAdminHandler : IRequestHandler<CreateFirstAdminCommand, ApplicationResult<CreateFirstAdminResponseDto>>
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CreateFirstAdminHandler> _logger;

        public CreateFirstAdminHandler(
            IVenueSubUserService subUserService,
            IApplicationDbContext context,
            ILogger<CreateFirstAdminHandler> logger)
        {
            _subUserService = subUserService;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<CreateFirstAdminResponseDto>> Handle(CreateFirstAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify no sub-users exist
                var hasSubUsers = await _context.VenueSubUsers
                    .AnyAsync(su => su.VenueId == request.VenueId, cancellationToken);

                if (hasSubUsers)
                {
                    return ApplicationResult<CreateFirstAdminResponseDto>.Failure("Sub-users already exist");
                }

                // Force admin role and full permissions for first admin
                var adminDto = new CreateVenueSubUserDto(
                    Username: request.Username,
                    Password: request.Password,
                    Role: VenueSubUserRole.Admin,
                    Permissions: VenuePermissions.AdminPermissions
                );

                var subUser = await _subUserService.CreateSubUserAsync(
                    request.VenueId,
                    null, // No creator for first admin
                    adminDto);

                // Mark as founder and update venue using EF entities directly
                var entity = await _context.VenueSubUsers.FindAsync(new object[] { subUser.Id }, cancellationToken);
                if (entity != null)
                {
                    entity.IsFounderAdmin = true;
                    entity.MustChangePassword = false; // First admin doesn't need to change password immediately
                }

                var venue = await _context.Venues.FindAsync(new object[] { request.VenueId }, cancellationToken);
                if (venue != null)
                {
                    venue.RequiresSubUserSetup = false;
                }

                await _context.SaveChangesAsync(cancellationToken);

                var response = new CreateFirstAdminResponseDto
                {
                    Success = true,
                    Message = "First admin created successfully",
                    SubUser = subUser,
                    NextStep = "sub-user-login"
                };

                return ApplicationResult<CreateFirstAdminResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first admin for venue {VenueId}", request.VenueId);
                return ApplicationResult<CreateFirstAdminResponseDto>.Failure("An error occurred while creating the first admin");
            }
        }
    }
}