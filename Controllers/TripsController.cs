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
using SmartFleet.ViewModel;
using SmartFleet.Services;

namespace SmartFleet.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly ITripStateManagementService _tripStateService;

        public TripsController(SmartFleetContext context, ITripStateManagementService tripStateService)
        {
            _context = context;
            _tripStateService = tripStateService;
        }

        // GET: Trips
        public async Task<IActionResult> Index(string? searchDriverName, VehicleType? vehicleType, string? destination, TripState? stateFilter, DateTime? startDate, DateTime? endDate)
        {
            // Update trip states automatically before displaying
            await _tripStateService.UpdateTripStatesAsync();

            var tripsQuery = _context.Trips.Include(t => t.Driver).Include(t => t.Order).Include(t => t.Vehicle).Include(t => t.CreatedByUser).AsQueryable();

            if (!string.IsNullOrEmpty(searchDriverName))
                tripsQuery = tripsQuery.Where(t => t.Driver != null && t.Driver.UserName.Contains(searchDriverName));
            if (vehicleType.HasValue)
                tripsQuery = tripsQuery.Where(t => t.Vehicle != null && t.Vehicle.Type == vehicleType);
            if (!string.IsNullOrEmpty(destination))
                tripsQuery = tripsQuery.Where(t => t.Order.Destination.Contains(destination));
            if (stateFilter.HasValue)
                tripsQuery = tripsQuery.Where(t => t.Status == stateFilter);
            if (startDate.HasValue)
                tripsQuery = tripsQuery.Where(t => t.Order.TripStartDate >= startDate);
            if (endDate.HasValue)
                tripsQuery = tripsQuery.Where(t => t.Order.TripEndDate <= endDate);

            var viewModel = new TripViewModel
            {
                Trips = await tripsQuery.ToListAsync(),
                SearchDriverName = searchDriverName,
                VehicleType = vehicleType,
                Destination = destination,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate
            };
            return View(viewModel);
        }

        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Update trip state before displaying details
            await _tripStateService.UpdateSingleTripStateAsync(id.Value);

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.Vehicle)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        public async Task<IActionResult> Create(int? id, string? userId)
        {
            if (id == null || string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Pass StartLocation and Destination to the view
            ViewBag.OrderStartLocation = order.StartLocation;
            ViewBag.OrderEndLocation = order.Destination;

            // فلترة المركبات حسب النوع ومتاحة فقط
            var filteredVehicles = await _context.Vehicles
                .Where(v => v.Type == order.VehicleType && v.Status == VehicleState.available)
                .ToListAsync();

            // فلترة السائقين المتاحين فقط (DriverStatus = active)
            var availableDrivers = await _context.Drivers
                .Where(d => d.DriverStatus == DriverState.active)
                .ToListAsync();

            // عرض اسم المستخدم (UserName) بدلاً من Id
            ViewBag.DriverId = new SelectList(availableDrivers, "Id", "UserName");
            ViewBag.VehicleId = new SelectList(filteredVehicles, "Id", "Model"); // لو عايز تعرض اسم الموديل مثلاً
            ViewBag.OrderId = id;
            ViewBag.CreatedBy = userId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,OrderId,DriverId,Status,CreatedBy")] Trip trip)
        {
            // التحقق من عدم وجود رحلة لهذا الطلب مسبقاً
            var existingTrip = await _context.Trips.AnyAsync(t => t.OrderId == trip.OrderId);
            if (existingTrip)
            {
                ModelState.AddModelError("OrderId", "يوجد رحلة مسجلة لهذا الطلب بالفعل");
            }

            trip.CreatedAt = DateTime.Now;
            trip.Distance = 0; // Initialize distance to 0, will be calculated automatically
            _context.Add(trip);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

            // إعادة تحميل بيانات العرض في حالة الخطأ
            ViewBag.DriverId = new SelectList(_context.Drivers, "Id", "UserName", trip.DriverId);
            ViewBag.VehicleId = new SelectList(_context.Vehicles, "Id", "Model", trip.VehicleId);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,OrderId,DriverId,Status,CreatedAt,CreatedBy")] Trip trip)
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
                .Include(t => t.CreatedByUser)
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

        // POST: Trips/RecalculateDistance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateDistance(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null)
            {
                return NotFound();
            }

            try
            {
                // Get all GPS locations for this vehicle during the trip period
                var tripStartTime = trip.Order.TripStartDate;
                var tripEndTime = trip.Order.TripEndDate;

                var locations = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == trip.VehicleId &&
                                vl.Timestamp >= tripStartTime &&
                                vl.Timestamp <= tripEndTime)
                    .OrderBy(vl => vl.Timestamp)
                    .ToListAsync();

                if (locations.Count < 2)
                {
                    trip.Distance = 0;
                }
                else
                {
                    decimal totalDistance = 0;
                    const double earthRadius = 6371; // Earth's radius in kilometers

                    // Calculate distance between consecutive GPS points
                    for (int i = 1; i < locations.Count; i++)
                    {
                        var prevLocation = locations[i - 1];
                        var currentLocation = locations[i];

                        // Only calculate distance if the vehicle is moving (speed > 0)
                        if (currentLocation.Speed > 0)
                        {
                            // Convert to radians
                            var lat1Rad = (double)prevLocation.Latitude * Math.PI / 180;
                            var lon1Rad = (double)prevLocation.Longitude * Math.PI / 180;
                            var lat2Rad = (double)currentLocation.Latitude * Math.PI / 180;
                            var lon2Rad = (double)currentLocation.Longitude * Math.PI / 180;

                            // Haversine formula
                            var dLat = lat2Rad - lat1Rad;
                            var dLon = lon2Rad - lon1Rad;
                            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                            var segmentDistance = (decimal)(earthRadius * c);
                            totalDistance += segmentDistance;
                        }
                    }

                    trip.Distance = Math.Round(totalDistance, 3);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Trip distance recalculated successfully. New distance: {trip.Distance} km";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error recalculating distance: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = trip.Id });
        }

        // POST: Trips/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }

            // Only allow cancellation of scheduled or in-progress trips
            if (trip.Status != TripState.Scheduled && trip.Status != TripState.InProgress)
            {
                TempData["ErrorMessage"] = "Only scheduled or in-progress trips can be cancelled.";
                return RedirectToAction(nameof(Details), new { id = trip.Id });
            }

            trip.Status = TripState.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trip cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
