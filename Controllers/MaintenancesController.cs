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
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    [Authorize]
    public class MaintenancesController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly IPaginationService _paginationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRoleService _userRoleService;
        private readonly ISearchService _searchService;

        public MaintenancesController(SmartFleetContext context, IPaginationService paginationService, UserManager<ApplicationUser> userManager, IUserRoleService userRoleService, ISearchService searchService)
        {
            _context = context;
            _paginationService = paginationService;
            _userManager = userManager;
            _userRoleService = userRoleService;
            _searchService = searchService;
        }

        public async Task<IActionResult> Index(string searchPlate, RepairState? statusFilter, PriorityDegree? priorityFilter, int pageNumber = 1)
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

            var userRoles = await _userRoleService.GetUserRoles(currentUser);
            var isMaintenanceManager = userRoles.Contains("MaintenanceManager");
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

            var filters = new List<System.Linq.Expressions.Expression<Func<Maintenance, bool>>>();
            if (!string.IsNullOrEmpty(searchPlate))
                filters.Add(m => m.Vehicle.LicensePlate.Contains(searchPlate));
            if (statusFilter.HasValue)
                filters.Add(m => m.RepairStatus == statusFilter.Value);
            if (priorityFilter.HasValue)
                filters.Add(m => m.Priority == priorityFilter.Value);
            query = _searchService.ApplyFilters(query, filters);

            int pageSize = 10;
            int totalCount = await query.CountAsync();
            
            // Sort by pending status first, then by high priority
            var sortedQuery = query.OrderBy(m => m.RepairStatus != RepairState.pending) // pending first
                                   .ThenBy(m => m.Priority != PriorityDegree.high) // high priority first
                                   .ThenByDescending(m => m.CreatedAt); // then by creation date
            
            var pagedMaintenances = await _paginationService.GetPaginatedAsync(sortedQuery, pageNumber, pageSize);
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.SearchPlate = searchPlate;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.PriorityFilter = priorityFilter;
            return View(pagedMaintenances);
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

            // Allow Fleet Managers and Maintenance Managers to view the page
            var userRoles = await _userRoleService.GetUserRoles(currentUser);
            var isMaintenanceManager = userRoles.Contains("MaintenanceManager");
            var isFleetManager = userRoles.Contains("FleetManager");
            
            if (!isMaintenanceManager && !isFleetManager)
            {
                TempData["ErrorMessage"] = "Only Fleet Managers and Maintenance Managers can access this page.";
                return RedirectToAction("Index", "Home");
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.Status == VehicleState.need_maintenance)
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

                // If maintenance status is completed, automatically change vehicle state to maintained
                if (maintenance.RepairStatus == RepairState.completed && maintenance.VehicleId.HasValue)
                {
                    var vehicle = await _context.Vehicles.FindAsync(maintenance.VehicleId.Value);
                    if (vehicle != null && vehicle.Status == VehicleState.need_maintenance)
                    {
                        vehicle.Status = VehicleState.maintained;
                        vehicle.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                        
                        TempData["SuccessMessage"] = $"Maintenance record created and vehicle {vehicle.LicensePlate} status automatically changed to Maintained.";
                    }
                }

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
                    // Get the original maintenance record to check if status is changing to completed
                    var originalMaintenance = await _context.Maintenances.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    bool isStatusChangingToCompleted = originalMaintenance != null && 
                                                     originalMaintenance.RepairStatus != RepairState.completed && 
                                                     maintenance.RepairStatus == RepairState.completed;

                    _context.Update(maintenance);
                    await _context.SaveChangesAsync();

                    // If maintenance status changed to completed, automatically change vehicle state to maintained
                    if (isStatusChangingToCompleted && maintenance.VehicleId.HasValue)
                    {
                        var vehicle = await _context.Vehicles.FindAsync(maintenance.VehicleId.Value);
                        if (vehicle != null && vehicle.Status == VehicleState.need_maintenance)
                        {
                            vehicle.Status = VehicleState.maintained;
                            vehicle.UpdatedAt = DateTime.Now;
                            await _context.SaveChangesAsync();
                            
                            TempData["SuccessMessage"] = $"Maintenance completed and vehicle {vehicle.LicensePlate} status automatically changed to Maintained.";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Maintenance record updated successfully.";
                    }
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
