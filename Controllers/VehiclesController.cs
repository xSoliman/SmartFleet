using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;

namespace SmartFleet.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly SmartFleetContext _context;

        public VehiclesController(SmartFleetContext context)
        {
            _context = context;
        }

        // GET: Vehicles + search & filter
        public async Task<IActionResult> Index(string searchModel, string searchPlate, VehicleType? typeFilter, VehicleState? stateFilter)
        {
            ViewData["PageTitle"] = "Vehicles";

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

       // [Authorize(Roles = "Fleet Administrator")]
        // GET: Vehicles/Create
        public IActionResult Create()
        {
            ViewData["PageTitle"] = "Vehicles";

            return View();
        }

        // POST: Vehicles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Model,Type,Capacity,LicensePlate,Status,Distance,CreatedAt")] Vehicle vehicle, IFormFile? imageFile)
        {
            ViewData["PageTitle"] = "Vehicles";

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/vehicles");
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    vehicle.VehicleImageUrl = "/uploads/vehicles/" + uniqueFileName; // Store relative path
                }
                else
                {
                    vehicle.VehicleImageUrl = "/assets/images/icons/download.png"; // Default image
                }

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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Model,Type,Capacity,LicensePlate,Status,Distance,CreatedAt,VehicleImageUrl")] Vehicle vehicle, IFormFile? imageFile)
        {
            ViewData["PageTitle"] = "Vehicles";

            if (id != vehicle.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/vehicles");
                        Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        vehicle.VehicleImageUrl = "/uploads/vehicles/" + uniqueFileName;
                    }

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
    }
}
