using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Models;
using SmartFleet.ViewModel;
using SmartFleet.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartFleet.Controllers
{
    // Optionally, add an authorization attribute if needed:
    // [Authorize(Roles = "Admin")]
    public class DriversController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public DriversController(SmartFleetContext context,
                                 UserManager<ApplicationUser > userManager,
                                 IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;  
        }

        // GET: Drivers
        public async Task<IActionResult> Index()
        {
            // Retrieve all drivers. Adjust as needed for filtering.
            var drivers = await _context.Users.OfType<Driver>().ToListAsync();
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

        public async Task<IActionResult> DriverDashboard()
        {
            string? driverId = Request.Cookies["UserId"];
            if (string.IsNullOrEmpty(driverId))
                return RedirectToAction("Login", "Account");

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == driverId);

            if (driver == null) return NotFound();

            // Load only trips assigned to this driver
            var trips = await _context.Trips
                .Where(t => t.DriverId == driverId)
                .ToListAsync();

            driver.Trips = trips;

            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(DriverState)));
            return View(driver);
        }





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



        public IActionResult Create()
        {
            return View();
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DriverViewModel model)
        {
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

                // Handle the profile image upload.
                if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
                {
                    // Define the folder path (e.g., wwwroot/images/profiles)
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "assets/images", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Use a unique file name (you may wish to use the driver's Id after creation)
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImageFile.CopyToAsync(fileStream);
                    }
                    // Store the relative URL in the ProfileImageUrl property.
                    driver.ProfileImageUrl = $"/assets/images/profiles/{uniqueFileName}";
                }

                // Create the identity user with a password.
                var result = await _userManager.CreateAsync(driver, model.Password);
                if (result.Succeeded)
                {
                    // Optionally, add the driver to a role
                    // await _userManager.AddToRoleAsync(driver, "Driver");

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
            var driver = await _userManager.FindByIdAsync(id) as Driver;
            if (driver != null)
            {
                var result = await _userManager.DeleteAsync(driver);
                // Optionally, delete the profile image from disk if needed.
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
