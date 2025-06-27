using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;

namespace SmartFleet.Controllers
{
    [Authorize]
    public class MaintenancesController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRoleService _userRoleService;

        public MaintenancesController(SmartFleetContext context, UserManager<ApplicationUser> userManager, IUserRoleService userRoleService)
        {
            _context = context;
            _userManager = userManager;
            _userRoleService = userRoleService;
        }

        public async Task<IActionResult> Index(string searchPlate, RepairState? statusFilter, PriorityDegree? priorityFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isMaintenanceManager = userRoles.Contains("MaintanceManager");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");

            var query = _context.Maintenances
                .Include(m => m.Vehicle)
                .Include(m => m.ReportedUser)
                .AsQueryable();

            // Role-based filtering
            if (isMaintenanceManager)
            {
                // Maintenance Manager sees all maintenance records
                query = query.AsQueryable();
            }
            else if (isFleetManager || isSysSupport)
            {
                // Fleet Manager and SysSupport see all maintenance records
                query = query.AsQueryable();
            }
            else
            {
                // Other roles have no access
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrEmpty(searchPlate))
            {
                query = query.Where(m => m.Vehicle.LicensePlate.Contains(searchPlate));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(m => m.RepairStatus == statusFilter.Value);
            }

            if (priorityFilter.HasValue)
            {
                query = query.Where(m => m.Priority == priorityFilter.Value);
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.ReportedUser)
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maintenance == null)
            {
                return NotFound();
            }

            return View(maintenance);
        }

        public async Task<IActionResult> MaintenanceVehicles()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.Status == VehicleState.need_maintenance || v.Status == VehicleState.under_maintenance)
                .ToListAsync();

            return View(vehicles);
        }

        [HttpGet]
        public async Task<IActionResult> CreateMaintenance(int? vehicleId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            // Check create permissions
            if (!await _userRoleService.CanCreateMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create maintenance records.";
                return RedirectToAction("Index");
            }

            var maintenance = new Maintenance
            {
                VehicleId = vehicleId ?? 0,
                ReportedBy = currentUser.Id,
                CreatedAt = DateTime.Now
            };

            // Load vehicle and user information for display
            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                var vehicle = await _context.Vehicles.FindAsync(vehicleId.Value);
                ViewBag.VehicleInfo = vehicle ?? null;
            }
            else
            {
                ViewBag.VehicleInfo = null;
            }

            ViewBag.UserInfo = currentUser;

            // Prepare dropdown data
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "LicensePlate");
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "UserName");

            return View("Create", maintenance);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVehicleStatus(int vehicleId, VehicleState newStatus)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null) return NotFound();

            vehicle.Status = newStatus;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MaintenanceVehicles));
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            // Check create permissions
            if (!await _userRoleService.CanCreateMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create maintenance records.";
                return RedirectToAction("Index");
            }

            // Initialize ViewBag data to prevent null reference exceptions
            ViewBag.VehicleInfo = null;
            ViewBag.UserInfo = currentUser;
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "LicensePlate");
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,ReportedBy,IssueDescription,RepairStatus,Priority,RepairedAt,CreatedAt")] Maintenance maintenance)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            // Check create permissions
            if (!await _userRoleService.CanCreateMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create maintenance records.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                maintenance.ReportedUser = await _context.Users.FindAsync(maintenance.ReportedBy);
                _context.Add(maintenance);
                await _context.SaveChangesAsync();

                // احصل على LicensePlate
                var licensePlate = await _context.Vehicles
                    .Where(v => v.Id == maintenance.VehicleId)
                    .Select(v => v.LicensePlate)
                    .FirstOrDefaultAsync();

                // إعادة التوجيه مع فلترة باللوحة
                return RedirectToAction("Index", new { searchPlate = licensePlate });
            }

            // Load vehicle and user information for display when validation fails
            if (maintenance.VehicleId > 0)
            {
                var vehicle = await _context.Vehicles.FindAsync(maintenance.VehicleId);
                ViewBag.VehicleInfo = vehicle ?? null;
            }
            else
            {
                ViewBag.VehicleInfo = null;
            }

            if (!string.IsNullOrEmpty(maintenance.ReportedBy))
            {
                var user = await _context.Users.FindAsync(maintenance.ReportedBy);
                ViewBag.UserInfo = user ?? null;
            }
            else
            {
                ViewBag.UserInfo = null;
            }

            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "LicensePlate");
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "UserName");
            return View(maintenance);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            // Check edit permissions
            if (!await _userRoleService.CanEditMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit maintenance records.";
                return RedirectToAction("Index");
            }

            var maintenance = await _context.Maintenances.FindAsync(id);
            if (maintenance == null)
            {
                return NotFound();
            }

            // Initialize ViewBag data to prevent null reference exceptions
            ViewBag.VehicleInfo = null;
            ViewBag.UserInfo = null;
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "LicensePlate", maintenance.VehicleId);
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "UserName", maintenance.ReportedBy);
            return View(maintenance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,ReportedBy,IssueDescription,RepairStatus,Priority,RepairedAt,CreatedAt")] Maintenance maintenance)
        {
            if (id != maintenance.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            // Check edit permissions
            if (!await _userRoleService.CanEditMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit maintenance records.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(maintenance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceExists(maintenance.Id))
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

            // Load vehicle and user information for display when validation fails
            if (maintenance.VehicleId > 0)
            {
                var vehicle = await _context.Vehicles.FindAsync(maintenance.VehicleId);
                ViewBag.VehicleInfo = vehicle ?? null;
            }
            else
            {
                ViewBag.VehicleInfo = null;
            }

            if (!string.IsNullOrEmpty(maintenance.ReportedBy))
            {
                var user = await _context.Users.FindAsync(maintenance.ReportedBy);
                ViewBag.UserInfo = user ?? null;
            }
            else
            {
                ViewBag.UserInfo = null;
            }

            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "LicensePlate", maintenance.VehicleId);
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "UserName", maintenance.ReportedBy);
            return View(maintenance);
        }
       
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.ReportedUser)
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maintenance == null)
            {
                return NotFound();
            }

            return View(maintenance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check access permissions
            if (!await _userRoleService.HasAccessToMaintenance(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to maintenance.";
                return RedirectToAction("Index", "Home");
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            string? licensePlate = null;

            if (maintenance != null)
            {
                licensePlate = maintenance.Vehicle?.LicensePlate;
                _context.Maintenances.Remove(maintenance);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { searchPlate = licensePlate });
        }

        private bool MaintenanceExists(int id)
        {
            return _context.Maintenances.Any(e => e.Id == id);
        }
    }
}
