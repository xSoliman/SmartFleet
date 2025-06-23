using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SmartFleet.Controllers
{
    [Authorize(Roles = "FleetManager,SysSupport")]
    public class GeofencesController : Controller
    {
        private readonly SmartFleetContext _context;
        public GeofencesController(SmartFleetContext context)
        {
            _context = context;
        }

        // GET: Geofences
        public async Task<IActionResult> Index()
        {
            var geofences = await _context.Geofence.ToListAsync();
            return View(geofences);
        }

        // GET: Geofences/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Geofences/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CenterLat,CenterLng,RadiusMeters")] Geofence geofence)
        {
            if (ModelState.IsValid)
            {
                _context.Add(geofence);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(geofence);
        }

        // GET: Geofences/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var geofence = await _context.Geofence.FindAsync(id);
            if (geofence == null) return NotFound();
            return View(geofence);
        }

        // POST: Geofences/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CenterLat,CenterLng,RadiusMeters")] Geofence geofence)
        {
            if (id != geofence.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(geofence);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(geofence);
        }

        // GET: Geofences/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var geofence = await _context.Geofence.FindAsync(id);
            if (geofence == null) return NotFound();
            return View(geofence);
        }

        // POST: Geofences/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var geofence = await _context.Geofence.FindAsync(id);
            if (geofence != null)
            {
                _context.Geofence.Remove(geofence);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Geofences/Assign/5
        public async Task<IActionResult> Assign(int? id)
        {
            if (id == null) return NotFound();
            var geofence = await _context.Geofence.FindAsync(id);
            if (geofence == null) return NotFound();
            var vehicles = await _context.Vehicles.ToListAsync();
            ViewBag.Geofence = geofence;
            return View(vehicles);
        }

        // POST: Geofences/AssignVehicle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignVehicle(int geofenceId, int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null) return NotFound();
            vehicle.GeofenceId = geofenceId;
            _context.Update(vehicle);
            await _context.SaveChangesAsync();
            return RedirectToAction("Assign", new { id = geofenceId });
        }
    }
} 