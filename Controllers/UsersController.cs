using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.ViewModel;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    [Authorize(Roles = "SysSupport")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SmartFleetContext _context;
        private readonly IUserRoleService _userRoleService;
        private readonly IPaginationService _paginationService;
        private readonly ISearchService _searchService;

        public UsersController(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            SmartFleetContext context,
            IUserRoleService userRoleService,
            IPaginationService paginationService,
            ISearchService searchService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _userRoleService = userRoleService;
            _paginationService = paginationService;
            _searchService = searchService;
        }

        // GET: Users/Index - عرض كل المستخدمين
        public async Task<IActionResult> Index(string searchUserName, string searchEmail, int pageNumber = 1)
        {
            int pageSize = 10;
            var usersQuery = _userManager.Users.AsQueryable();
            var filters = new List<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>();
            if (!string.IsNullOrEmpty(searchUserName))
                filters.Add(u => u.UserName.Contains(searchUserName));
            if (!string.IsNullOrEmpty(searchEmail))
                filters.Add(u => u.Email.Contains(searchEmail));
            usersQuery = _searchService.ApplyFilters(usersQuery, filters).OrderBy(u => u.UserName);
            int totalCount = await usersQuery.CountAsync();
            var pagedUsers = await _paginationService.GetPaginatedAsync(usersQuery, pageNumber, pageSize);

            var userViewModels = new List<UserItemViewModel>();
            var userRoles = new Dictionary<string, List<string>>();
            var driverDetails = new Dictionary<string, DriverDetailsViewModel>();

            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var driverInfo = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == user.Id);
                
                // إضافة معلومات المستخدم
                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.AccountStatus,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList(),
                    IsDriver = driverInfo != null,
                    LicenseNumber = driverInfo?.LicenseNumber,
                    LicenseExpiryDate = driverInfo?.LicenseExpiryDate,
                    DriverStatus = driverInfo?.DriverStatus
                });

                // إضافة أدوار المستخدم
                userRoles[user.Id] = roles.ToList();

                // إضافة تفاصيل السائق إذا كان سائقاً
                if (driverInfo != null)
                {
                    driverDetails[user.Id] = new DriverDetailsViewModel
                    {
                        LicenseNumber = driverInfo.LicenseNumber ?? "",
                        LicenseExpiryDate = driverInfo.LicenseExpiryDate,
                        DriverStatus = driverInfo.DriverStatus
                    };
                }
            }

            // إنشاء ViewModel الرئيسي
            var viewModel = new UserManagementViewModel
            {
                TotalUsers = totalCount,
                ActiveUsers = userViewModels.Count(u => u.IsActive),
                InactiveUsers = userViewModels.Count(u => !u.IsActive),
                DriversCount = userViewModels.Count(u => u.IsDriver),
                Users = userViewModels,
                UserRoles = userRoles,
                DriverDetails = driverDetails
            };

            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.SearchUserName = searchUserName;
            ViewBag.SearchEmail = searchEmail;
            return View(viewModel);
        }

        // GET: Users/ManageRoles - إدارة أدوار المستخدمين
        public async Task<IActionResult> ManageRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoleViewModels = new List<UserRoleManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleViewModels.Add(new UserRoleManagementViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    CurrentRoles = roles.ToList()
                });
            }

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(userRoleViewModels);
        }

        // POST: Users/AddRole - إضافة دور للمستخدم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageRoles));
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            { 
                TempData["ErrorMessage"] = "Role does not exist.";
                return RedirectToAction(nameof(ManageRoles));
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role {roleName} has been successfully added to user {user.UserName}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add role.";
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // POST: Users/RemoveRole - إزالة دور من المستخدم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageRoles));
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role {roleName} has been successfully removed from user {user.UserName}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove role.";
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // GET: Users/CreateUser - صفحة إنشاء مستخدم جديد
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToListAsync();

            return View(new CreateUserViewModel());
        }

        // POST: Users/CreateUser - إنشاء مستخدم جديد
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user;

                // إنشاء مستخدم عادي أو سائق حسب الدور
                if (model.IsDriver)
                {
                    user = new Driver
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        AccountStatus = true,
                        CreatedAt = DateTime.Now,
                        LicenseNumber = model.LicenseNumber,
                        LicenseExpiryDate = model.LicenseExpiryDate ?? DateTime.Now.AddYears(5),
                        DriverStatus = DriverState.Available
                    };
                }
                else
                {
                    user = new ApplicationUser
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        AccountStatus = true,
                        CreatedAt = DateTime.Now
                    };
                }

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // إضافة الأدوار المحددة
                    if (model.SelectedRoles != null && model.SelectedRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    TempData["SuccessMessage"] = "User created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // إعادة تحميل البيانات في حالة الخطأ
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToListAsync();

            return View(model);
        }

        // POST: Users/ToggleAccountStatus - تفعيل/إلغاء تفعيل المستخدم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAccountStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            user.AccountStatus = !user.AccountStatus;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var status = user.AccountStatus ? "activated" : "deactivated";
                TempData["SuccessMessage"] = $"User {user.UserName} has been successfully {status}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update account status.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/DeleteUser - حذف المستخدم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User {user.UserName} has been successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 