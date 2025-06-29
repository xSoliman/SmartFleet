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
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    [Authorize(Roles = "FleetManager,SysSupport")]
    public class VehiclesController : BaseController
    {
        private readonly SmartFleetContext _context;
        private readonly IPaginationService _paginationService;
        private readonly ISearchService _searchService;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;

        public VehiclesController(SmartFleetContext context, UserManager<ApplicationUser> userManager, IUserRoleService userRoleService, IPaginationService paginationService, ISearchService searchService, INotificationService notificationService) 
            : base(userManager, userRoleService)
        {
            _context = context;
            _paginationService = paginationService;
            _searchService = searchService;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
        }

        // GET: Vehicles + search & filter
        public async Task<IActionResult> Index(string searchModel, string searchPlate, VehicleType? typeFilter, VehicleState? stateFilter, int pageNumber = 1)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            int pageSize = 10; // Fixed page size
            var vehicles = _context.Vehicles.AsQueryable();
            var filters = new List<System.Linq.Expressions.Expression<Func<Vehicle, bool>>>();
            if (!string.IsNullOrEmpty(searchModel))
                filters.Add(v => v.Model.Contains(searchModel));
            if (!string.IsNullOrEmpty(searchPlate))
                filters.Add(v => v.LicensePlate.Contains(searchPlate));
            if (typeFilter.HasValue)
                filters.Add(v => v.Type == typeFilter);
            if (stateFilter.HasValue)
                filters.Add(v => v.Status == stateFilter);
            vehicles = _searchService.ApplyFilters(vehicles, filters);
            int totalCount = await vehicles.CountAsync();
            var pagedVehicles = await _paginationService.GetPaginatedAsync(vehicles.OrderBy(v => v.Model), pageNumber, pageSize);

            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.SearchModel = searchModel;
            ViewBag.SearchPlate = searchPlate;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.StateFilter = stateFilter;

            return View(pagedVehicles);
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewData["PageTitle"] = "Vehicles";
            
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return RedirectToAction("AccessDenied", "Account");
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

        // GET: Vehicles/Create
        public async Task<IActionResult> Create()
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return RedirectToAction("AccessDenied", "Account");
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
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
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
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ImageUpload", "An error occurred while uploading the vehicle image. Please try again or contact support.");
                        Console.WriteLine($"Vehicle image upload error: {ex}");
                        return View(vehicle);
                    }
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
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.SimCard)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Model,Type,Capacity,LicensePlate,Status,TotalDistanceTraveled,RegistrationExpiryDate,CreatedAt,VehicleImageUrl,SimCardId")] Vehicle vehicle, IFormFile? imageFile)
        {
            ViewData["PageTitle"] = "Vehicles";

            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id != vehicle.Id)
            {
                return NotFound();
            }

            // Get current user and roles for validation
            var currentUser = await GetCurrentUserAsync();
            var userRoles = await GetCurrentUserRolesAsync();
            var isFleetManager = userRoles.Contains("FleetManager");
            var isMaintenanceManager = userRoles.Contains("MaintenanceManager");
            var isSysSupport = userRoles.Contains("SysSupport");

            // Get the original vehicle state for comparison
            var originalVehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (originalVehicle == null)
            {
                return NotFound();
            }

            // Role-based validation for state changes
            if (isFleetManager)
            {
                // Fleet Manager can only change between available, need_maintenance, and maintained
                if (vehicle.Status != VehicleState.available && 
                    vehicle.Status != VehicleState.need_maintenance && 
                    vehicle.Status != VehicleState.maintained)
                {
                    ModelState.AddModelError("Status", "Fleet Manager can only change vehicle state to Available, Need Maintenance, or Maintained.");
                }
            }
            else if (isMaintenanceManager)
            {
                // Maintenance Manager can only change to maintained
                if (vehicle.Status != VehicleState.maintained)
                {
                    ModelState.AddModelError("Status", "Maintenance Manager can only change vehicle state to Maintained.");
                }
            }
            else if (!isSysSupport)
            {
                // Other roles cannot change vehicle status
                ModelState.AddModelError("Status", "You don't have permission to change vehicle status.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing vehicle to preserve the current total distance and SimCard assignment
                    var existingVehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                    if (existingVehicle != null)
                    {
                        // Add the user-entered initial distance to the existing total distance
                        vehicle.TotalDistanceTraveled = existingVehicle.TotalDistanceTraveled + vehicle.TotalDistanceTraveled;
                        
                        // Preserve the SimCard assignment if not explicitly changed
                        if (vehicle.SimCardId == null)
                        {
                            vehicle.SimCardId = existingVehicle.SimCardId;
                        }
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
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
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("ImageUpload", "An error occurred while uploading the vehicle image. Please try again or contact support.");
                            Console.WriteLine($"Vehicle image upload error: {ex}");
                            return View(vehicle);
                        }
                    }

                    vehicle.UpdatedAt = DateTime.Now;

                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();

                    // Check if vehicle status changed to need_maintenance and notify maintenance managers
                    if (originalVehicle.Status != VehicleState.need_maintenance && vehicle.Status == VehicleState.need_maintenance)
                    {
                        await NotifyMaintenanceManagersForVehicleNeedingMaintenance(vehicle, currentUser);
                    }

                    TempData["SuccessMessage"] = $"Vehicle {vehicle.LicensePlate} updated successfully.";
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
                return RedirectToAction(nameof(Edit), new { id = vehicle.Id });
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
                return RedirectToAction("AccessDenied", "Account");
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
                return RedirectToAction("AccessDenied", "Account");
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
                return RedirectToAction("AccessDenied", "Account");
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
                return RedirectToAction("AccessDenied", "Account");
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
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.VehicleLocations)
                .Include(v => v.SimCard)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // POST: Vehicles/AssignSimCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSimCard(int vehicleId, int simCardId)
        {
            // Debug logging
            
            try
            {
                // Check if user has access to vehicles
                if (!await HasAccessToVehiclesAsync())
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    TempData["ErrorMessage"] = "Vehicle not found.";
                    return RedirectToAction("SimCard", new { id = vehicleId });
                }

                var simCard = await _context.SimCards.FindAsync(simCardId);
                if (simCard == null)
                {
                    TempData["ErrorMessage"] = "SimCard not found.";
                    return RedirectToAction("SimCard", new { id = vehicleId });
                }

                // Check if SimCard is already assigned to another vehicle
                var existingVehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.SimCardId == simCardId && v.Id != vehicleId);
                if (existingVehicle != null)
                {
                    TempData["ErrorMessage"] = $"SimCard {simCard.SimNumber} is already assigned to vehicle {existingVehicle.LicensePlate}.";
                    return RedirectToAction("SimCard", new { id = vehicleId });
                }

                // Assign SimCard to vehicle
                vehicle.SimCardId = simCardId;
                vehicle.UpdatedAt = DateTime.Now;
                
                _context.Update(vehicle);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"SimCard {simCard.SimNumber} successfully assigned to vehicle {vehicle.LicensePlate}.";
                return RedirectToAction("SimCard", new { id = vehicleId });
            }
            catch (ArgumentNullException ex) when (ex.ParamName == "user")
            {
                TempData["ErrorMessage"] = "You must be logged in to perform this action.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning SimCard: {ex.Message}";
                return RedirectToAction("SimCard", new { id = vehicleId });
            }
        }

        // POST: Vehicles/RemoveSimCard/5
        [HttpPost]
        public async Task<IActionResult> RemoveSimCard(int id)
        {
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return Unauthorized();
            }

            try
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    return NotFound();
                }

                vehicle.SimCardId = null;
                vehicle.UpdatedAt = DateTime.Now;
                
                _context.Update(vehicle);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "SimCard removed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: api/simcards/available
        [HttpGet("api/simcards/available")]
        public async Task<IActionResult> GetAvailableSimCards()
        {
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return Unauthorized();
            }

            try
            {
                var availableSimCards = await _context.SimCards
                    .Where(s => s.Status == SimCardStatus.Active)
                    .Select(s => new
                    {
                        s.Id,
                        s.SimNumber,
                        s.Carrier,
                        s.Status,
                        IsAssigned = _context.Vehicles.Any(v => v.SimCardId == s.Id)
                    })
                    .Where(s => !s.IsAssigned) // Only unassigned SimCards
                    .ToListAsync();

                return Json(availableSimCards);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/vehicles/available
        [HttpGet("api/vehicles/available")]
        public async Task<IActionResult> GetAvailableVehicles()
        {
            // Check if user has access to vehicles
            if (!await HasAccessToVehiclesAsync())
            {
                return Unauthorized();
            }

            try
            {
                var availableVehicles = await _context.Vehicles
                    .Where(v => v.SimCardId == null) // Only vehicles without SimCards
                    .Select(v => new
                    {
                        v.Id,
                        v.Model,
                        v.LicensePlate,
                        v.Type,
                        v.Status
                    })
                    .ToListAsync();

                return Json(availableVehicles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // API: Get geofence info for a vehicle
        [HttpGet("api/vehicles/{id}/geofence")]
        public async Task<IActionResult> GetVehicleGeofence(int id)
        {
            var vehicle = await _context.Vehicles.Include(v => v.Geofence).FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null)
                return NotFound();
            if (vehicle.Geofence == null)
                return Json(new { hasGeofence = false });
            return Json(new {
                hasGeofence = true,
                geofence = new {
                    id = vehicle.Geofence.Id,
                    name = vehicle.Geofence.Name,
                    type = vehicle.Geofence.Type.ToString(),
                    centerLat = vehicle.Geofence.CenterLat,
                    centerLng = vehicle.Geofence.CenterLng,
                    radiusMeters = vehicle.Geofence.RadiusMeters,
                    polygonJson = vehicle.Geofence.PolygonJson
                }
            });
        }

        // GET: Vehicles/Geofence/5
        public async Task<IActionResult> Geofence(int? id)
        {
            if (id == null) return NotFound();
            var vehicle = await _context.Vehicles.Include(v => v.Geofence).FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null) return NotFound();
            var geofences = await _context.Geofences.ToListAsync();
            ViewBag.Geofences = geofences;
            return View(vehicle);
        }

        // POST: Vehicles/AssignGeofence
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignGeofence(int vehicleId, int geofenceId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null) return NotFound();
            vehicle.GeofenceId = geofenceId;
            _context.Update(vehicle);
            await _context.SaveChangesAsync();
            return RedirectToAction("Geofence", new { id = vehicleId });
        }

        // POST: Vehicles/RemoveGeofence
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveGeofence(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null) return NotFound();
            vehicle.GeofenceId = null;
            _context.Update(vehicle);
            await _context.SaveChangesAsync();
            return RedirectToAction("Geofence", new { id = vehicleId });
        }

        // Private method to notify maintenance managers when a vehicle needs maintenance
        private async Task NotifyMaintenanceManagersForVehicleNeedingMaintenance(Vehicle vehicle, ApplicationUser currentUser)
        {
            try
            {
                // Get all maintenance managers
                var maintenanceManagers = await _userRoleService.GetUsersByRole("MaintenanceManager");
                
                if (maintenanceManagers.Any())
                {
                    var notificationTitle = "Vehicle Needs Maintenance";
                    var notificationMessage = $"Vehicle {vehicle.LicensePlate} ({vehicle.Model}) has been marked as needing maintenance by {currentUser.UserName}.";

                    // Send notification to each maintenance manager
                    foreach (var manager in maintenanceManagers)
                    {
                        await _notificationService.CreateNotificationAsync(
                            manager.Id,
                            notificationTitle,
                            notificationMessage,
                            RelatedTable.Vehicle,
                            vehicle.Id
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the vehicle update
                Console.WriteLine($"Error sending maintenance notification: {ex.Message}");
            }
        }
    }
}