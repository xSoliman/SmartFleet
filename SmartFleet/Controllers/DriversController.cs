using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Models;
using SmartFleet.ViewModel;
using SmartFleet.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    // Add authorization attribute to restrict access to FleetManager and SysSupport roles only
    [Authorize(Roles = "FleetManager,SysSupport")]
    public class DriversController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;
        private readonly IPaginationService _paginationService;
        private readonly ISearchService _searchService;
        private readonly IReferenceCheckService _referenceCheckService;
        private readonly IValidationService _validationService;

        public DriversController(SmartFleetContext context,
                                 UserManager<ApplicationUser> userManager,
                                 IWebHostEnvironment env,
                                 INotificationService notificationService,
                                 IUserRoleService userRoleService,
                                 IPaginationService paginationService,
                                 ISearchService searchService,
                                 IReferenceCheckService referenceCheckService,
                                 IValidationService validationService)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
            _paginationService = paginationService;
            _searchService = searchService;
            _referenceCheckService = referenceCheckService;
            _validationService = validationService;
        }

        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> DriverDashboard()
        {
            string? driverId = User.FindFirst(ClaimTypes.NameIdentifier).ToString();
            if (string.IsNullOrEmpty(driverId))
                return RedirectToAction("Login", "Account");

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == driverId);

            if (driver == null) return NotFound();

            // Load only trips assigned to this driver
            var trips = await _paginationService.GetPaginatedAsync(
                _context.Trips
                    .Where(t => t.DriverId == driverId)
                    .OrderByDescending(t => t.CreatedAt), 1, 10);

            driver.Trips = trips;

            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(DriverState)));
            return View(driver);
        }

        [Authorize(Roles = "FleetManager,SysSupport")]
        [HttpPost]
        public async Task<IActionResult> UpdateDriverStatus(string id, DriverState status)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == id);
            if (driver == null) return NotFound();

            driver.DriverStatus = status;
            _context.Update(driver);
            await _context.SaveChangesAsync();

            return RedirectToAction("DriverDashboard");
        }

        // GET: Drivers
        public async Task<IActionResult> Index(string searchUserName, string searchLicense, DriverState? statusFilter, int pageNumber = 1)
        {
            int pageSize = 10;
            var driversQuery = _context.Users.OfType<Driver>().AsQueryable();
            var filters = new List<System.Linq.Expressions.Expression<Func<Driver, bool>>>();
            if (!string.IsNullOrEmpty(searchUserName))
                filters.Add(d => d.UserName.Contains(searchUserName));
            if (!string.IsNullOrEmpty(searchLicense))
                filters.Add(d => d.LicenseNumber.Contains(searchLicense));
            if (statusFilter.HasValue)
                filters.Add(d => d.DriverStatus == statusFilter.Value);
            driversQuery = _searchService.ApplyFilters(driversQuery, filters);
            int totalCount = await driversQuery.CountAsync();
            var drivers = await _paginationService.GetPaginatedAsync(driversQuery.OrderBy(d => d.UserName), pageNumber, pageSize);
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.SearchUserName = searchUserName;
            ViewBag.SearchLicense = searchLicense;
            ViewBag.StatusFilter = statusFilter;
            return View(drivers);
        }

        // GET: Drivers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Users.OfType<Driver>()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // GET: Drivers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DriverViewModel model)
        {
            // Validate uniqueness
            if (!await _validationService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }

            if (!await _validationService.IsDriverLicenseUniqueAsync(model.LicenseNumber))
            {
                ModelState.AddModelError("LicenseNumber", "This license number is already registered.");
            }

            if (ModelState.IsValid)
            {
                // Create a new Driver instance and set Identity fields.
                var driver = new Driver
                {
                    Email = model.Email,
                    UserName = model.UserName,
                    LicenseNumber = model.LicenseNumber,
                    LicenseExpiryDate = model.LicenseExpiryDate,
                    DriverStatus = model.DriverStatus,
                    // Default status and created time are set in the model.
                    AccountStatus = true,
                    CreatedAt = DateTime.Now
                };

                // Handle profile image if uploaded.
                if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
                {
                    try
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "assets/images", "profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProfileImageFile.CopyToAsync(fileStream);
                        }
                        driver.ProfileImageUrl = $"/assets/images/profiles/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ImageUpload", "An error occurred while uploading the image. Please try again or contact support.");
                        Console.WriteLine($"Driver image upload error: {ex}");
                        return View(model);
                    }
                }

                // Create the user with the password.
                var result = await _userManager.CreateAsync(driver, model.Password);
                if (result.Succeeded)
                {
                    // Assign the Driver role to the user.
                    await _userManager.AddToRoleAsync(driver, "Driver");
                    
                    // Notifications removed - no longer sending notifications for driver creation
                    
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _userManager.FindByIdAsync(id) as Driver;
            if (driver == null)
            {
                return NotFound();
            }

            // Populate view model with existing values.
            var model = new DriverViewModel
            {
                Id = driver.Id,
                Email = driver.Email,
                UserName = driver.UserName,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                DriverStatus = driver.DriverStatus,
                ImageUrl = driver.ProfileImageUrl,
                // Do not pre-populate the password
            };

            return View(model);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, DriverViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var driver = await _userManager.FindByIdAsync(id) as Driver;
            if (driver == null)
            {
                return NotFound();
            }

            // Validate uniqueness (excluding current driver)
            if (!await _validationService.IsEmailUniqueAsync(model.Email, driver.Id))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }

            if (!await _validationService.IsDriverLicenseUniqueAsync(model.LicenseNumber, driver.Id))
            {
                ModelState.AddModelError("LicenseNumber", "This license number is already registered.");
            }

            if (ModelState.IsValid)
            {
                // Update basic fields.
                driver.Email = model.Email;
                driver.UserName = model.UserName;
                driver.LicenseNumber = model.LicenseNumber;
                driver.LicenseExpiryDate = model.LicenseExpiryDate;
                driver.DriverStatus = model.DriverStatus;

                // Handle profile image if a new file is uploaded.
                if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
                {
                    try
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "assets/images", "profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProfileImageFile.CopyToAsync(fileStream);
                        }
                        // Optionally, delete the old image file if needed.
                        driver.ProfileImageUrl = $"/assets/images/profiles/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ImageUpload", "An error occurred while uploading the image. Please try again or contact support.");
                        Console.WriteLine($"Driver image upload error: {ex}");
                        return View(model);
                    }
                }

                // Handle password change if a new password is provided.
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    // Remove the existing password.
                    var removePassResult = await _userManager.RemovePasswordAsync(driver);
                    if (!removePassResult.Succeeded)
                    {
                        foreach (var error in removePassResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }

                    // Add the new password.
                    var addPassResult = await _userManager.AddPasswordAsync(driver, model.Password);
                    if (!addPassResult.Succeeded)
                    {
                        foreach (var error in addPassResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }

                // Update the user in the identity store.
                var result = await _userManager.UpdateAsync(driver);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        // GET: Drivers/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Users.OfType<Driver>()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var (canDelete, message) = await _referenceCheckService.CanDeleteDriverAsync(id);
            if (!canDelete)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            var driver = await _userManager.FindByIdAsync(id) as Driver;
            if (driver != null)
            {
                var result = await _userManager.DeleteAsync(driver);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Driver deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete driver.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
