using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;

namespace SmartFleet.Controllers
{
    public class VehiclesController : BaseController
    {
        private readonly SmartFleetContext _context;

        public VehiclesController(SmartFleetContext context, UserManager<ApplicationUser> userManager, IUserRoleService userRoleService) 
            : base(userManager, userRoleService)
        {
            _context = context;
        }

        // GET: Vehicles + search & filter
        public async Task<IActionResult> Index(string searchModel, string searchPlate, VehicleType? typeFilter, VehicleState? stateFilter)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to view vehicles.";
                return RedirectToAction("Index", "Home");
            }

            var vehicles = _context.Vehicles.AsQueryable();

            if (!string.IsNullOrEmpty(searchModel))
                vehicles = vehicles.Where(v => v.Model.Contains(searchModel));

            if (!string.IsNullOrEmpty(searchPlate))
                vehicles = vehicles.Where(v => v.LicensePlate.Contains(searchPlate));

            if (typeFilter.HasValue)
                vehicles = vehicles.Where(v => v.Type == typeFilter);

            if (stateFilter.HasValue)
                vehicles = vehicles.Where(v => v.Status == stateFilter);

            return View(await vehicles.ToListAsync());
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";
            
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to view vehicle details.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // GET: Vehicles/Create
        public async Task<IActionResult> Create()
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to create vehicles.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Model,Type,Capacity,LicensePlate,Status,TotalDistanceTraveled,RegistrationExpiryDate,CreatedAt")] Vehicle vehicle, IFormFile? imageFile)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to create vehicles.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/vehicles");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    vehicle.VehicleImageUrl = "/uploads/vehicles/" + uniqueFileName;
                }
                else
                {
                    vehicle.VehicleImageUrl = "/assets/images/icons/download.png";
                }

                vehicle.CreatedAt = DateTime.Now;
                vehicle.UpdatedAt = DateTime.Now;
                // The user-entered TotalDistanceTraveled is treated as the initial distance
                // and becomes the starting total distance for the new vehicle

                _context.Add(vehicle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to edit vehicles.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Model,Type,Capacity,LicensePlate,Status,TotalDistanceTraveled,RegistrationExpiryDate,CreatedAt,VehicleImageUrl")] Vehicle vehicle, IFormFile? imageFile)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to edit vehicles.";
                return RedirectToAction("Index", "Home");
            }

            if (id != vehicle.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing vehicle to preserve the current total distance
                    var existingVehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                    if (existingVehicle != null)
                    {
                        // Add the user-entered initial distance to the existing total distance
                        vehicle.TotalDistanceTraveled = existingVehicle.TotalDistanceTraveled + vehicle.TotalDistanceTraveled;
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/vehicles");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        vehicle.VehicleImageUrl = "/uploads/vehicles/" + uniqueFileName;
                    }

                    vehicle.UpdatedAt = DateTime.Now;

                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to delete vehicles.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to delete vehicles.";
                return RedirectToAction("Index", "Home");
            }

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }

        // GET: Vehicles/Maintenance/5
        public async Task<IActionResult> Maintenance(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";
            
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to view vehicle maintenance.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Maintenances)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // GET: Vehicles/SimCard/5
        public async Task<IActionResult> SimCard(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";
            
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to view vehicle simcard.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.SimCard)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // GET: Vehicles/Tracking/5
        public async Task<IActionResult> Tracking(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";
            
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to view vehicle tracking.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.VehicleLocations)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }
    }
}
