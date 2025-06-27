using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Models;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.ViewComponents
{
    public class UserActionsViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRoleService _userRoleService;

        public UserActionsViewComponent(UserManager<ApplicationUser> userManager, IUserRoleService userRoleService)
        {
            _userManager = userManager;
            _userRoleService = userRoleService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentUser = await _userManager.GetUserAsync(UserClaimsPrincipal);
            
            var userActionsModel = new UserActionsViewModel
            {
                HasAccessToDashboard = await _userRoleService.HasAccessToDashboard(currentUser),
                IsAuthenticated = User.Identity.IsAuthenticated
            };

            return View(userActionsModel);
        }
    }

    public class UserActionsViewModel
    {
        public bool HasAccessToDashboard { get; set; }
        public bool IsAuthenticated { get; set; }
    }
} 