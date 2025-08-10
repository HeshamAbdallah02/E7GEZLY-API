using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service implementation for venue authorization logic
/// Contains complex business rules for venue access control and permissions
/// </summary>
public sealed class VenueAuthorizationService : IVenueAuthorizationService
{
    public async Task<AuthorizationResult> CheckPermissionAsync(VenueSubUser subUser, VenuePermissions requiredPermission, string action)
    {
        // Check if sub-user is active
        if (!subUser.IsActive)
            return AuthorizationResult.Failure("Sub-user account is inactive");

        // Check if sub-user is locked out
        if (subUser.IsLockedOut())
            return AuthorizationResult.Failure("Sub-user account is locked out");

        // Founder admin has all permissions
        if (subUser.IsFounderAdmin)
            return AuthorizationResult.Success();

        // Check if sub-user must change password
        if (subUser.MustChangePassword)
            return AuthorizationResult.Failure("Password change required before accessing resources");

        // Check specific permission
        if (!subUser.HasPermission(requiredPermission))
            return AuthorizationResult.Failure($"Missing required permission: {requiredPermission} for action: {action}");

        // Additional role-based checks
        var roleValidation = await ValidateRoleForActionAsync(subUser.Role, requiredPermission, action);
        if (!roleValidation.IsValid)
            return AuthorizationResult.Failure($"Role '{subUser.Role}' is not authorized for this action: {string.Join(", ", roleValidation.Errors)}");

        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> CanManageSubUserAsync(VenueSubUser manager, VenueSubUser target, string operation)
    {
        // Check if manager is active and not locked out
        var basicCheck = await CheckPermissionAsync(manager, VenuePermissions.ViewSubUsers, "manage sub-user");
        if (!basicCheck.IsAuthorized)
            return basicCheck;

        // Founder admin can manage all sub-users except cannot delete themselves
        if (manager.IsFounderAdmin)
        {
            if (operation == "delete" && manager.Id == target.Id)
                return AuthorizationResult.Failure("Founder admin cannot delete their own account");
            return AuthorizationResult.Success();
        }

        // Cannot manage founder admin
        if (target.IsFounderAdmin)
            return AuthorizationResult.Failure("Cannot manage founder admin account");

        // Cannot manage self for certain operations
        if (manager.Id == target.Id)
        {
            var selfManagedOperations = new[] { "view", "update_own_password" };
            if (!selfManagedOperations.Contains(operation.ToLowerInvariant()))
                return AuthorizationResult.Failure("Cannot perform this operation on your own account");
        }

        // Role hierarchy: Admins can manage coworkers, but coworkers cannot manage admins
        if (manager.Role == VenueSubUserRole.Coworker && target.Role == VenueSubUserRole.Admin)
            return AuthorizationResult.Failure("Coworkers cannot manage admin accounts");

        // Check specific operation permissions
        switch (operation.ToLowerInvariant())
        {
            case "create":
                return await CheckPermissionAsync(manager, VenuePermissions.CreateSubUsers, operation);
            case "update":
            case "edit":
                return await CheckPermissionAsync(manager, VenuePermissions.EditSubUsers, operation);
            case "delete":
            case "deactivate":
                return await CheckPermissionAsync(manager, VenuePermissions.DeleteSubUsers, operation);
            case "reset_password":
                return await CheckPermissionAsync(manager, VenuePermissions.ResetSubUserPasswords, operation);
            case "view":
                return await CheckPermissionAsync(manager, VenuePermissions.ViewSubUsers, operation);
            default:
                return AuthorizationResult.Failure($"Unknown operation: {operation}");
        }
    }

    public async Task<AuthorizationResult> CanAccessVenueResourceAsync(VenueSubUser subUser, Guid venueId)
    {
        // Check basic authentication status
        if (!subUser.IsActive)
            return AuthorizationResult.Failure("Sub-user account is inactive");

        if (subUser.IsLockedOut())
            return AuthorizationResult.Failure("Sub-user account is locked out");

        // Check if sub-user belongs to the venue
        if (subUser.VenueId != venueId)
            return AuthorizationResult.Failure("Sub-user does not belong to this venue");

        return AuthorizationResult.Success();
    }

    public async Task<VenuePermissions> GetEffectivePermissionsAsync(VenueSubUser subUser)
    {
        // Founder admin has all permissions
        if (subUser.IsFounderAdmin)
            return VenuePermissions.AdminPermissions;

        // Inactive or locked out users have no permissions
        if (!subUser.IsActive || subUser.IsLockedOut())
            return VenuePermissions.None;

        // Users who must change password only have permission to change password
        if (subUser.MustChangePassword)
            return VenuePermissions.None; // Could add a specific "ChangePassword" permission

        // Return assigned permissions modified by role constraints
        return await ApplyRoleConstraintsAsync(subUser.Role, subUser.Permissions);
    }

    public async Task<ValidationResult> ValidatePermissionsForRoleAsync(VenueSubUserRole role, VenuePermissions permissions)
    {
        var errors = new List<string>();

        switch (role)
        {
            case VenueSubUserRole.Admin:
                // Admins should have comprehensive permissions
                var recommendedAdminPermissions = VenuePermissions.ViewVenueDetails | 
                                                 VenuePermissions.EditVenueDetails |
                                                 VenuePermissions.ManagePricing |
                                                 VenuePermissions.ManageWorkingHours |
                                                 VenuePermissions.ViewSubUsers |
                                                 VenuePermissions.CreateSubUsers |
                                                 VenuePermissions.EditSubUsers |
                                                 VenuePermissions.ViewBookings |
                                                 VenuePermissions.ManageCustomers |
                                                 VenuePermissions.ViewReports;

                if ((permissions & recommendedAdminPermissions) != recommendedAdminPermissions)
                {
                    errors.Add("Admin role should have comprehensive management permissions");
                }
                break;

            case VenueSubUserRole.Coworker:
                // Coworkers should not have sensitive permissions
                var restrictedCoworkerPermissions = VenuePermissions.DeleteSubUsers |
                                                   VenuePermissions.ManageFinancials |
                                                   VenuePermissions.ProcessRefunds;

                if ((permissions & restrictedCoworkerPermissions) != VenuePermissions.None)
                {
                    errors.Add("Coworker role should not have delete, financial, or refund permissions");
                }

                // Coworkers must have basic operational permissions
                var requiredCoworkerPermissions = VenuePermissions.ViewVenueDetails |
                                                 VenuePermissions.ViewBookings;

                if ((permissions & requiredCoworkerPermissions) != requiredCoworkerPermissions)
                {
                    errors.Add("Coworker role must have basic viewing permissions");
                }
                break;

            default:
                errors.Add($"Unknown role: {role}");
                break;
        }

        return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }

    private async Task<ValidationResult> ValidateRoleForActionAsync(VenueSubUserRole role, VenuePermissions requiredPermission, string action)
    {
        var errors = new List<string>();

        // Some permissions are restricted by role regardless of assignment
        switch (role)
        {
            case VenueSubUserRole.Coworker:
                var coworkerRestrictedPermissions = new[]
                {
                    VenuePermissions.DeleteSubUsers,
                    VenuePermissions.ManageFinancials,
                    VenuePermissions.ProcessRefunds
                };

                if (coworkerRestrictedPermissions.Contains(requiredPermission))
                {
                    errors.Add($"Coworker role cannot perform action '{action}' requiring permission '{requiredPermission}'");
                }
                break;
        }

        return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }

    private async Task<VenuePermissions> ApplyRoleConstraintsAsync(VenueSubUserRole role, VenuePermissions assignedPermissions)
    {
        switch (role)
        {
            case VenueSubUserRole.Admin:
                // Admins can have all assigned permissions
                return assignedPermissions;

            case VenueSubUserRole.Coworker:
                // Remove restricted permissions for coworkers
                var restrictedPermissions = VenuePermissions.DeleteSubUsers |
                                          VenuePermissions.ManageFinancials |
                                          VenuePermissions.ProcessRefunds;
                
                return assignedPermissions & ~restrictedPermissions;

            default:
                return VenuePermissions.None;
        }
    }
}