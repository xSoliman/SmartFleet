using Microsoft.AspNetCore.Identity;
using SmartFleet.Models;

namespace SmartFleet.Services
{
    public interface IUserRoleService
    {
        Task<bool> HasAccessToVehicles(ApplicationUser user);
        Task<bool> HasAccessToDrivers(ApplicationUser user);
        Task<bool> HasAccessToMaintenance(ApplicationUser user);
        Task<bool> HasAccessToOrders(ApplicationUser user);
        Task<bool> HasAccessToTrips(ApplicationUser user);
        Task<bool> HasAccessToTracking(ApplicationUser user);
        Task<bool> HasAccessToDashboard(ApplicationUser user);
        Task<List<string>> GetUserRoles(ApplicationUser user);
        Task<List<ApplicationUser>> GetUsersByRole(string roleName);
    }

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
            return roles.Any(r => r == "SysSupport" || r == "FleetManager" || r == "MaintanceManager");
        }

        public async Task<bool> HasAccessToOrders(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager" || r == "MaintanceManager" || r == "commissioner" || r == "NormalUser" || r == "Driver");
        }

        public async Task<bool> HasAccessToTrips(ApplicationUser user)
        {
            if (user == null) return false;
            var roles = await GetUserRoles(user);
            return roles.Any(r => r == "SysSupport" || r == "FleetManager" || r == "MaintanceManager" || r == "commissioner" || r == "NormalUser" || r == "Driver");
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
    }
} 