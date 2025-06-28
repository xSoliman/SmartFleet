using Microsoft.AspNetCore.Identity;
using SmartFleet.Models;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
   

    public class UserRoleService : IUserRoleService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRoleService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            if (user == null) return new List<string>();
            return (await _userManager.GetRolesAsync(user)).ToList();
        }

        public async Task<List<ApplicationUser>> GetUsersByRole(string roleName)
        {
            return (await _userManager.GetUsersInRoleAsync(roleName)).ToList();
        }

        public async Task<bool> HasAccessToVehicles(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager");
        }

        public async Task<bool> HasAccessToDrivers(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager");
        }

        public async Task<bool> HasAccessToMaintenance(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager" || r == "MaintenanceManager");
        }

        public async Task<bool> HasAccessToOrders(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // NormalUser, Commissioner, FleetManager, SysSupport have access to orders
            return roles.Any(r => r == "SysSupport" || r == "FleetManager" || r == "commissioner" || r == "NormalUser");
        }

        public async Task<bool> HasAccessToTrips(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // NormalUser, Driver, FleetManager, SysSupport have access to trips
            return roles.Any(r => r == "SysSupport" || r == "FleetManager" || r == "NormalUser" || r == "Driver");
        }

        public async Task<bool> HasAccessToTracking(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager");
        }

        public async Task<bool> HasAccessToDashboard(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager");
        }

        public async Task<bool> HasAccessToReports(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager");
        }

        // New methods for order permissions
        public async Task<bool> CanCreateOrder(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // NormalUser, Driver, FleetManager can create orders
            // Commissioner and MaintenanceManager cannot create orders
            return roles.Any(r => r == "NormalUser" || r == "Driver" || r == "FleetManager" || r == "SysSupport");
        }

        public async Task<bool> CanEditOrder(ApplicationUser user, OrderState orderStatus)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            
            // Only pending orders can be edited
            if (orderStatus != OrderState.Pending) return false;
            
            // NormalUser can edit their own pending orders
            // FleetManager can edit any pending order
            // SysSupport can edit any pending order
            return roles.Any(r => r == "NormalUser" || r == "FleetManager" || r == "SysSupport");
        }

        public async Task<bool> CanCancelOrder(ApplicationUser user, OrderState orderStatus)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            
            // Only pending orders can be cancelled
            if (orderStatus != OrderState.Pending) return false;
            
            // NormalUser can cancel their own pending orders
            // FleetManager can cancel any pending order
            // SysSupport can cancel any pending order
            return roles.Any(r => r == "NormalUser" || r == "FleetManager" || r == "SysSupport");
        }

        public async Task<bool> CanApproveRejectOrder(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // Only Commissioner can approve/reject orders (SysSupport cannot)
            return roles.Any(r => r == "commissioner");
        }

        // New methods for trip permissions
        public async Task<bool> CanCreateTrip(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // Only FleetManager and SysSupport can create trips
            return roles.Any(r => r == "FleetManager" || r == "SysSupport");
        }

        public async Task<bool> CanEditTrip(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // Only FleetManager and SysSupport can edit trips
            return roles.Any(r => r == "FleetManager" || r == "SysSupport");
        }

        public async Task<bool> CanCancelTrip(ApplicationUser user, TripState tripStatus)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            
            // Only scheduled trips can be cancelled
            if (tripStatus != TripState.Scheduled) return false;
            
            // NormalUser can cancel scheduled trips from their orders
            // FleetManager can cancel any scheduled trip
            // SysSupport can cancel any scheduled trip
            return roles.Any(r => r == "NormalUser" || r == "FleetManager" || r == "SysSupport");
        }

        // New methods for maintenance permissions
        public async Task<bool> CanCreateMaintenance(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // Only MaintenanceManager and SysSupport can create maintenance records
            // FleetManager cannot create maintenance records
            return roles.Any(r => r == "MaintenanceManager" || r == "SysSupport");
        }

        public async Task<bool> CanEditMaintenance(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            // Only MaintenanceManager and SysSupport can edit maintenance records
            // FleetManager cannot edit maintenance records
            return roles.Any(r => r == "MaintenanceManager" || r == "SysSupport");
        }
    }
} 