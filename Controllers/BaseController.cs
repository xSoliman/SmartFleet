using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Models;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly IUserRoleService _userRoleService;

        protected BaseController(UserManager<ApplicationUser> userManager, IUserRoleService userRoleService)
        {
            _userManager = userManager;
            _userRoleService = userRoleService;
        }

        protected async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        protected async Task<List<string>> GetCurrentUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            return await _userRoleService.GetUserRoles(user);
        }

        protected async Task<bool> HasAccessToVehiclesAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToVehicles(user);
        }

        protected async Task<bool> HasAccessToDriversAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToDrivers(user);
        }

        protected async Task<bool> HasAccessToMaintenanceAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToMaintenance(user);
        }

        protected async Task<bool> HasAccessToOrdersAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToOrders(user);
        }

        protected async Task<bool> HasAccessToTripsAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToTrips(user);
        }

        protected async Task<bool> HasAccessToTrackingAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToTracking(user);
        }

        protected async Task<bool> HasAccessToDashboardAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;
            return await _userRoleService.HasAccessToDashboard(user);
        }
    }
} 