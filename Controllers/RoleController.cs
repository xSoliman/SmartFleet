using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.ViewModel;

namespace SmartFleet.Controllers
{
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;

        public RoleController(RoleManager<IdentityRole> roleManager)
        {
            this.roleManager = roleManager;
        }
        public IActionResult AddRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveRole(RoleViewModel role)
        {

            if (ModelState.IsValid)
            {
                // save in DB
                IdentityRole identityRole = new IdentityRole();
                identityRole.Name = role.RoleName;
                IdentityResult res = await roleManager.CreateAsync(identityRole);
                if (res.Succeeded)
                {
                    ViewBag.success = true;// check in view if true return any message 
                    return View("AddRole");
                }
                foreach (var item in res.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View("AddRole", role);
        }
    }
    
}