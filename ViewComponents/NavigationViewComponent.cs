using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Models;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.ViewComponents
{
    public class NavigationViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRoleService _userRoleService;

        public NavigationViewComponent(UserManager<ApplicationUser> userManager, IUserRoleService userRoleService)
        {
            _userManager = userManager;
            _userRoleService = userRoleService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentUser = await _userManager.GetUserAsync(UserClaimsPrincipal);
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            
            var navigationModel = new NavigationViewModel
            {
                // Always show all options, but track access permissions
                HasAccessToVehicles = isAuthenticated && await _userRoleService.HasAccessToVehicles(currentUser),
                HasAccessToDrivers = isAuthenticated && await _userRoleService.HasAccessToDrivers(currentUser),
                HasAccessToMaintenance = isAuthenticated && await _userRoleService.HasAccessToMaintenance(currentUser),
                HasAccessToOrders = isAuthenticated && await _userRoleService.HasAccessToOrders(currentUser),
                HasAccessToTrips = isAuthenticated && await _userRoleService.HasAccessToTrips(currentUser),
                HasAccessToTracking = isAuthenticated && await _userRoleService.HasAccessToTracking(currentUser),
                HasAccessToDashboard = isAuthenticated && await _userRoleService.HasAccessToDashboard(currentUser),
                HasAccessToReports = isAuthenticated && await _userRoleService.HasAccessToReports(currentUser),
                IsAuthenticated = isAuthenticated,
                CurrentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? "",
                CurrentAction = ViewContext.RouteData.Values["action"]?.ToString() ?? ""
            };

            return View(navigationModel);
        }
    }

    public class NavigationViewModel
    {
        public bool HasAccessToVehicles { get; set; }
        public bool HasAccessToDrivers { get; set; }
        public bool HasAccessToMaintenance { get; set; }
        public bool HasAccessToOrders { get; set; }
        public bool HasAccessToTrips { get; set; }
        public bool HasAccessToTracking { get; set; }
        public bool HasAccessToDashboard { get; set; }
        public bool HasAccessToReports { get; set; }
        public bool IsAuthenticated { get; set; }
        public string CurrentController { get; set; } = "";
        public string CurrentAction { get; set; } = "";
    }
} 