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

                    return RedirectToAction("Login");
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

                // Check if user account is active
                if (!applicationUser.AccountStatus)
                {
                    ModelState.AddModelError("", "Your account has been deactivated. Please contact your administrator.");
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

                // Role-based redirect after successful login
                var primaryRole = roles.FirstOrDefault();
                return RedirectToRoleBasedPage(primaryRole);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "An error occurred during login.");
                return View("Login", User);
            }
        }

        private IActionResult RedirectToRoleBasedPage(string? role)
        {
            return role?.ToLower() switch
            {
                "fleetmanager" or "syssupport" => RedirectToAction("Dashboard", "Home"),
                "maintenancemanager" => RedirectToAction("Index", "Maintenances"),
                "commissioner" => RedirectToAction("Index", "Orders"),
                "driver" => RedirectToAction("Index", "Trips"),
                "normaluser" or _ => RedirectToAction("Index", "Home")
            };
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login");
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

            // If user is a driver, get driver-specific information
            if (roles.Contains("Driver"))
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == user.Id);
                if (driver != null)
                {
                    viewModel.LicenseNumber = driver.LicenseNumber;
                    viewModel.LicenseExpiryDate = driver.LicenseExpiryDate;
                    viewModel.DriverStatus = driver.DriverStatus;
                }
            }

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

            var roles = await userManager.GetRolesAsync(user);
            var isDriver = roles.Contains("Driver");

            var viewModel = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ImageUrl = user.ProfileImageUrl,
                IsDriver = isDriver
            };

            // If user is a driver, get driver-specific information
            if (isDriver)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == user.Id);
                if (driver != null)
                {
                    viewModel.LicenseNumber = driver.LicenseNumber;
                    viewModel.LicenseExpiryDate = driver.LicenseExpiryDate;
                    viewModel.DriverStatus = driver.DriverStatus;
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? ImageFile)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get user roles to determine if they are a driver
            var roles = await userManager.GetRolesAsync(user);
            var isDriver = roles.Contains("Driver");
            model.IsDriver = isDriver;

            if (!ModelState.IsValid)
            {
                // If user is a driver, populate driver information for the view
                if (isDriver)
                {
                    var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == user.Id);
                    if (driver != null)
                    {
                        model.LicenseNumber = driver.LicenseNumber;
                        model.LicenseExpiryDate = driver.LicenseExpiryDate;
                        model.DriverStatus = driver.DriverStatus;
                    }
                }
                return View(model);
            }

            // Update user fields
            user.UserName = model.UserName;
            user.PhoneNumber = model.PhoneNumber;

            // Handle image update (if uploaded)
            if (ImageFile != null)
            {
                try
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
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageUpload", "An error occurred while uploading the image. Please try again or contact support.");
                    Console.WriteLine($"Image upload error: {ex}");
                    return View(model);
                }
            }
            else if (model.RemoveImage) // If user wants to remove image
            {
                user.ProfileImageUrl = null;
            }

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // If user is a driver, update driver information
                if (model.IsDriver)
                {
                    var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == user.Id);
                    if (driver != null)
                    {
                        driver.LicenseNumber = model.LicenseNumber;
                        driver.LicenseExpiryDate = model.LicenseExpiryDate ?? DateTime.MinValue;
                        await _context.SaveChangesAsync();
                    }
                }

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

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
