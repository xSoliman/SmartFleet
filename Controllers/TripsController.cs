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
    public class TripsController : Controller
    {
        private readonly SmartFleetContext _context;

        public TripsController(SmartFleetContext context)
        {
            _context = context;
        }

        // GET: Trips
        public async Task<IActionResult> Index()
        {
            var smartFleetContext = _context.Trips.Include(t => t.Driver).Include(t => t.Order).Include(t => t.Vehicle).Include(t => t.admin);
            return View(await smartFleetContext.ToListAsync());
        }

        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.Vehicle)
                .Include(t => t.admin)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        public IActionResult Create(int? id, string? userId)
        {
            if (id == null || string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            ViewData["DriverId"] = new SelectList(_context.Drivers, "Id", "Id");
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id");

            ViewBag.OrderId = id;
            ViewBag.CreatedBy = userId; // تمرير الـ UserId بشكل صحيح

            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,OrderId,DriverId,StartTime,EndTime,StartLocation,EndLocation,Distance,Status,CreatedBy")] Trip trip)
        {
            if (ModelState.IsValid)
            {
                trip.CreatedAt = DateTime.Now; // تعيين وقت الإنشاء
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["DriverId"] = new SelectList(_context.Drivers, "Id", "Id", trip.DriverId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", trip.OrderId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id", trip.VehicleId);

            return View(trip);
        }



        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }
            ViewData["DriverId"] = new SelectList(_context.Drivers, "Id", "Id", trip.DriverId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", trip.OrderId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id", trip.VehicleId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Id", trip.CreatedBy);
            return View(trip);
        }

        // POST: Trips/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,OrderId,DriverId,StartTime,EndTime,StartLocation,EndLocation,Distance,Status,CreatedAt,CreatedBy")] Trip trip)
        {
            if (id != trip.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripExists(trip.Id))
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
            ViewData["DriverId"] = new SelectList(_context.Drivers, "Id", "Id", trip.DriverId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", trip.OrderId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Id", trip.VehicleId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Id", trip.CreatedBy);
            return View(trip);
        }

        // GET: Trips/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.Vehicle)
                .Include(t => t.admin)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        // POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.Id == id);
        }
    }
}
