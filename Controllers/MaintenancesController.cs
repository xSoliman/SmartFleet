using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;

namespace SmartFleet.Controllers
{
    public class MaintenancesController : Controller
    {
        private readonly SmartFleetContext _context;

        public MaintenancesController(SmartFleetContext context)
        {
            _context = context;
        }

       
        public async Task<IActionResult> Index(string searchLicensePlate)
        {
            var query = _context.Maintenances.Include(m => m.Vehicle).Include(m => m.ReportedUser).AsQueryable();

            if (!string.IsNullOrEmpty(searchLicensePlate))
            {
                query = query.Where(m => m.Vehicle.LicensePlate.Contains(searchLicensePlate));
            }

            return View(await query.ToListAsync());
        }

        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
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

        
        public IActionResult Create()
        {
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,ReportedBy,IssueDescription,RepairStatus,Priority,RepairedAt,CreatedAt")] Maintenance maintenance)
        {
            if (ModelState.IsValid)
            {
                // Assign ReportedUser based on ReportedBy (User ID)
                maintenance.ReportedUser = await _context.Users.FindAsync(maintenance.ReportedBy);

                _context.Add(maintenance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "UserName"); // Use UserName or another user-friendly field
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "LicensePlate"); // Use LicensePlate or another user-friendly field
            return View();
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances.FindAsync(id);
            if (maintenance == null)
            {
                return NotFound();
            }
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "Id", maintenance.ReportedBy);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id", maintenance.VehicleId);
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
            ViewData["ReportedBy"] = new SelectList(_context.Users, "Id", "Id", maintenance.ReportedBy);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id", maintenance.VehicleId);
            return View(maintenance);
        }
       
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
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
            var maintenance = await _context.Maintenances.FindAsync(id);
            if (maintenance != null)
            {
                _context.Maintenances.Remove(maintenance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaintenanceExists(int id)
        {
            return _context.Maintenances.Any(e => e.Id == id);
        }
    }
}
