using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    public class SimCardsController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;
        private readonly IPaginationService _paginationService;
        private readonly ISearchService _searchService;

        public SimCardsController(SmartFleetContext context, INotificationService notificationService, IUserRoleService userRoleService, IPaginationService paginationService, ISearchService searchService)
        {
            _context = context;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
            _paginationService = paginationService;
            _searchService = searchService;
        }

        // GET: SimCards
        //search & filter
        public async Task<IActionResult> Index(string searchSimNumber, string searchCarrier, string statusFilter, int pageNumber = 1)
        {
            var simCards = _context.SimCards.AsQueryable();
            var filters = new List<System.Linq.Expressions.Expression<Func<SimCard, bool>>>();
            if (!string.IsNullOrEmpty(searchSimNumber))
                filters.Add(s => s.SimNumber.Contains(searchSimNumber));
            if (!string.IsNullOrEmpty(searchCarrier))
                filters.Add(s => s.Carrier.Contains(searchCarrier));
            if (Enum.TryParse<SimCardStatus>(statusFilter, out var parsedStatus))
                filters.Add(s => s.Status == parsedStatus);
            simCards = _searchService.ApplyFilters(simCards, filters);
            int pageSize = 10;
            int totalCount = await simCards.CountAsync();
            var pagedSimCards = await _paginationService.GetPaginatedAsync(simCards.OrderBy(s => s.SimNumber), pageNumber, pageSize);

            // Get vehicle assignment information for each SimCard
            var simCardIds = pagedSimCards.Select(s => s.Id).ToList();
            var vehicleAssignments = await _context.Vehicles
                .Where(v => v.SimCardId.HasValue && simCardIds.Contains(v.SimCardId.Value))
                .Select(v => new { v.SimCardId, v.Id, v.LicensePlate, v.Model, v.Type })
                .ToListAsync();

            ViewBag.VehicleAssignments = vehicleAssignments.ToDictionary(v => v.SimCardId.Value, v => v);
            
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.SearchSimNumber = searchSimNumber;
            ViewBag.SearchCarrier = searchCarrier;
            ViewBag.StatusFilter = statusFilter;
            return View(pagedSimCards);
        }

        // GET: SimCards/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var simCard = await _context.SimCards
                .FirstOrDefaultAsync(m => m.Id == id);
            if (simCard == null)
            {
                return NotFound();
            }

            return View(simCard);
        }

        // GET: SimCards/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SimCards/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SimNumber,Carrier,Status,CreatedAt")] SimCard simCard)
        {
            if (ModelState.IsValid)
            {
                _context.Add(simCard);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(simCard);
        }

        // GET: SimCards/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var simCard = await _context.SimCards.FindAsync(id);
            if (simCard == null)
            {
                return NotFound();
            }
            return View(simCard);
        }

        // POST: SimCards/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SimNumber,Carrier,Status,CreatedAt")] SimCard simCard)
        {
            if (id != simCard.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original SimCard from DB
                    var originalSimCard = await _context.SimCards.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    bool statusChanged = originalSimCard != null && originalSimCard.Status != simCard.Status;

                    _context.Update(simCard);
                    await _context.SaveChangesAsync();

                    // --- Notification Logic ---
                    if (statusChanged)
                    {
                        var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                        string title = "SimCard Status Changed";
                        string message = $"SimCard {simCard.SimNumber} status changed to {simCard.Status}.";
                        foreach (var user in fleetManagers)
                        {
                            await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.SimCard, simCard.Id);
                        }
                    }
                    // --- End Notification Logic ---
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SimCardExists(simCard.Id))
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
            return View(simCard);
        }

        // GET: SimCards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var simCard = await _context.SimCards
                .FirstOrDefaultAsync(m => m.Id == id);
            if (simCard == null)
            {
                return NotFound();
            }

            return View(simCard);
        }

        // POST: SimCards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var simCard = await _context.SimCards.FindAsync(id);
            if (simCard != null)
            {
                _context.SimCards.Remove(simCard);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SimCardExists(int id)
        {
            return _context.SimCards.Any(e => e.Id == id);
        }

        // GET: SimCards/GetAvailableVehicles
        public async Task<IActionResult> GetAvailableVehicles()
        {
            try
            {
                var allVehicles = await _context.Vehicles
                    .Select(v => new
                    {
                        v.Id,
                        v.Model,
                        v.LicensePlate,
                        v.Type,
                        v.Status,
                        v.SimCardId,
                        IsAssigned = v.SimCardId.HasValue
                    })
                    .ToListAsync();

                return Json(allVehicles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: SimCards/AssignToVehicle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToVehicle(int simCardId, int vehicleId)
        {
            try
            {
                var simCard = await _context.SimCards.FindAsync(simCardId);
                if (simCard == null)
                {
                    TempData["ErrorMessage"] = "SimCard not found.";
                    return RedirectToAction(nameof(Index));
                }

                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    TempData["ErrorMessage"] = "Vehicle not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if SimCard is already assigned to another vehicle
                var existingVehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.SimCardId == simCardId && v.Id != vehicleId);
                if (existingVehicle != null)
                {
                    // If it's assigned to a different vehicle, we'll reassign it
                    existingVehicle.SimCardId = null;
                    existingVehicle.UpdatedAt = DateTime.Now;
                    _context.Update(existingVehicle);
                }

                // Check if vehicle already has a SimCard
                if (vehicle.SimCardId.HasValue)
                {
                    TempData["WarningMessage"] = $"Vehicle {vehicle.LicensePlate} already has a SimCard assigned. The previous assignment will be removed.";
                }

                // Assign SimCard to vehicle
                vehicle.SimCardId = simCardId;
                vehicle.UpdatedAt = DateTime.Now;
                
                _context.Update(vehicle);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"SimCard {simCard.SimNumber} successfully assigned to vehicle {vehicle.LicensePlate}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning SimCard: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SimCards/RemoveAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignment(int simCardId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.SimCardId == simCardId);
                
                if (vehicle != null)
                {
                    vehicle.SimCardId = null;
                    vehicle.UpdatedAt = DateTime.Now;
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "SimCard assignment removed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "SimCard is not currently assigned to any vehicle.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error removing assignment: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
