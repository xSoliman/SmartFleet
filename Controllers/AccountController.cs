using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Common;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.ViewModel;

namespace SmartFleet.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly SmartFleetContext _context;


        public AccountController(SmartFleetContext context, UserManager<ApplicationUser> userManager,
       SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _context = context;
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRegister(RegisterViewModel User)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Register", User);
                }

                var newUser = new ApplicationUser
                {
                    UserName = User.UserName,
                    Email = User.Email,
                    PhoneNumber = User.PhoneNumber
                };
                var result = await userManager.CreateAsync(newUser, User.Password);

                if (result.Succeeded)
                {

                    await userManager.AddToRoleAsync(newUser, Roles.NormalUser.ToString());

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("RegisterError", error.Description);
                }
                return View("Register", User);
            }
            catch (Exception ex)
            {
                
                ModelState.AddModelError("RegisterError", "An error occurred during registration. Please try again.");
                return View("Register", User);
            }
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLogin(LoginViewModel User)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Login", User);
                }

                var applicationUser = await userManager.FindByEmailAsync(User.Email);
                if (applicationUser == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View("Login", User);
                }

                var passwordValid = await userManager.CheckPasswordAsync(applicationUser, User.Password);
                if (!passwordValid)
                {
                    ModelState.AddModelError("", "Email or Password Wrong.");
                    return View("Login", User);
                }

                await signInManager.SignInAsync(applicationUser, User.RememberMe);

                var claims = new List<Claim>
        {
            new Claim("Email", applicationUser.Email),
            new Claim("UserName", applicationUser.UserName),
            new Claim(ClaimTypes.NameIdentifier, applicationUser.Id)
        };
                var roles = await userManager.GetRolesAsync(applicationUser);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
                await userManager.AddClaimsAsync(applicationUser, claims); 

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "An error occurred during login.");
                return View("Login", User);
            }
        }



        public async Task<IActionResult> LogOut()
        {
            await signInManager.SignOutAsync();
            return View("Login");
        }

        public async Task<IActionResult> MyAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var roles = await userManager.GetRolesAsync(user);

            var lastOrder = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            var viewModel = new MyAccountViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "No Role",
                ImageUrl = user.ProfileImageUrl,
                OrderStatus = lastOrder?.Status 
            };

            return View(viewModel);
        }





        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var viewModel = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ImageUrl = user.ProfileImageUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? ImageFile, bool RemoveImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Update user fields
            user.UserName = model.UserName;
            user.PhoneNumber = model.PhoneNumber;

            // Handle image update (if uploaded)
            if (ImageFile != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets/images");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                user.ProfileImageUrl = "/assets/images/" + uniqueFileName;
            }
            else if (RemoveImage) // If user wants to remove image
            {
                user.ProfileImageUrl = null;
            }

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("MyAccount");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }



        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await signInManager.RefreshSignInAsync(user);
                return RedirectToAction("MyAccount");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


    }
}
