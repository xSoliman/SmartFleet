using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AccountController(SmartFleetContext context, UserManager<ApplicationUser> userManager,
       SignInManager<ApplicationUser> signInManager, IHttpContextAccessor httpContextAccessor)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _context = context;
            _httpContextAccessor = httpContextAccessor; // حفظ الكائن
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        /// <summary>
        ///
        //
        public async Task<IActionResult> Fleet_Manager()
        {
            var fleetManager = await _context.Users.FirstOrDefaultAsync(u => u.Id == "5");

            if (fleetManager == null)
            {
                fleetManager = new ApplicationUser
                {
                    Id = "5",
                    UserName = "FleetManager",
                    Email = "fleet@smartfleet.com",
                    ProfileImageUrl = "Available"
                };
                _context.Users.Add(fleetManager);
                await _context.SaveChangesAsync();
            }

            var pendingOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == OrderState.approved)
                .ToListAsync();

            var viewModel = new FleetManagerViewModel
            {
                FleetManager = fleetManager,
                PendingOrders = pendingOrders
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Fleet_Manager(FleetManagerViewModel model)
        {
            var fleetManager = await _context.Users.FirstOrDefaultAsync(u => u.Id == "5");

            if (fleetManager != null)
            {
                fleetManager.ProfileImageUrl = model.FleetManager.ProfileImageUrl;
                _context.Users.Update(fleetManager);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Fleet_Manager));
        }


        public async Task<IActionResult> Index()
        {
            var fleetManager = await _context.Users.FirstOrDefaultAsync(u => u.Id == "5");

            if (fleetManager == null)
            {
                fleetManager = new ApplicationUser
                {
                    Id = "5",
                    UserName = "FleetManager",
                    Email = "fleet@smartfleet.com",
                    ProfileImageUrl = "Available"
                };
                _context.Users.Add(fleetManager);
                await _context.SaveChangesAsync();
            }

            var pendingOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == OrderState.pending)
                .ToListAsync();

            var viewModel = new FleetManagerViewModel
            {
                FleetManager = fleetManager,
                PendingOrders = pendingOrders
            };

            return View("Broker_Manager", viewModel); // توجيه إلى View باسم "Broker_Manager"
        }


        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = OrderState.approved;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = OrderState.rejected;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> SaveRegister(RegisterViewModel User)
        {
            if (ModelState.IsValid)
            {
                //string role = User.UserType == UserType.Student ? "Student" : "Instructor";
                // Mapping
                ApplicationUser appUser = new ApplicationUser();
                appUser.UserName = User.UserName;
                appUser.Email = User.Email;
                // appUser.PasswordHash = User.Password;
                appUser.PhoneNumber = User.PhoneNumber;
                    
                // save DB
                IdentityResult identityResult = await userManager.CreateAsync(appUser, User.Password);// hash password
                if (identityResult.Succeeded)
                {
                   // await userManager.AddToRoleAsync(appUser, "Fleet Administrator");
                    // Create Cookies after Register

                    await signInManager.SignInAsync(appUser, false);
                    return RedirectToAction("Login");
                }

                foreach (var item in identityResult.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View("Register", User);
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
            if (ModelState.IsValid)
            {
                var applicationUser = await userManager.FindByEmailAsync(User.Email);
                if (User.Email == "sayed@smartfleet.com")
                {
                    return RedirectToAction("Fleet_Manager","Account");
                }
                if (User.Email == "sayed1@smartfleet.com")
                {
                    return RedirectToAction("Index", "Account");
                }

                if (applicationUser != null)
                {
                    bool found = await userManager.CheckPasswordAsync(applicationUser, User.Password);
                    if (found)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(7), // Cookie valid for 7 days
                            HttpOnly = true,
                            IsEssential = true
                        };
                        Response.Cookies.Append("UserId", applicationUser.Id, cookieOptions);

                        await signInManager.SignInAsync(applicationUser, User.RememberMe);
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Email or Password Wrong");
            }
            return View("Login", User);
        }




        public async Task<IActionResult> LogOut()
        {
            await signInManager.SignOutAsync();
            return View("Login");
        }

        public async Task<IActionResult> MyAccount()
        {
            // جلب UserId من الكوكيز
            var userId = Request.Cookies["UserId"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // البحث عن المستخدم بناءً على الـ UserId
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var roles = await userManager.GetRolesAsync(user);

            // التحقق مما إذا كان لديه طلبات سابقة
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
                OrderStatus = lastOrder?.Status // تعيين الحالة فقط إذا كان هناك طلب
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
