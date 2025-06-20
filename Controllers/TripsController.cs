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
using SmartFleet.ViewModel;
using SmartFleet.Services;

namespace SmartFleet.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly ITripStateManagementService _tripStateService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TripsController(SmartFleetContext context, ITripStateManagementService tripStateService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _tripStateService = tripStateService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string destination, string searchDriverName, VehicleType? vehicleType, TripState? stateFilter, DateTime? startDate, DateTime? endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var isDriver = userRoles.Contains("Driver");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSystemSupport = userRoles.Contains("SystemSupport");

            IQueryable<Trip> tripsQuery = _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.CreatedByUser);

            List<Trip> assignedTrips = new List<Trip>();

            if (isFleetManager || isSystemSupport)
            {
                // Fleet Managers and System Support see all trips
            }
            else if (isDriver)
            {
                // Drivers see trips assigned to them in a separate list
                assignedTrips = await tripsQuery.Where(t => t.DriverId == currentUser.Id).ToListAsync();
                
                // And also see trips from orders they created (if any)
                tripsQuery = tripsQuery.Where(t => t.Order.UserId == currentUser.Id);
            }
            else
            {
                // Other users see trips from orders they created
                tripsQuery = tripsQuery.Where(t => t.Order.UserId == currentUser.Id);
            }
            
            // Apply filters
            if (!string.IsNullOrEmpty(destination))
            {
                tripsQuery = tripsQuery.Where(t => t.Order.Destination.Contains(destination));
            }
            if (!string.IsNullOrEmpty(searchDriverName))
            {
                tripsQuery = tripsQuery.Where(t => t.Driver.UserName.Contains(searchDriverName));
            }
            if (vehicleType.HasValue)
            {
                tripsQuery = tripsQuery.Where(t => t.Vehicle.Type == vehicleType.Value);
            }
            if (stateFilter.HasValue)
            {
                tripsQuery = tripsQuery.Where(t => t.Status == stateFilter.Value);
            }
            if (startDate.HasValue)
            {
                tripsQuery = tripsQuery.Where(t => t.Order.TripStartDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                tripsQuery = tripsQuery.Where(t => t.Order.TripEndDate <= endDate.Value);
            }

            var filteredTrips = await tripsQuery.ToListAsync();

            // Custom sorting
            var sortedTrips = filteredTrips.OrderBy(t => t.Status switch {
                TripState.InProgress => 0,
                TripState.Scheduled => 1,
                TripState.Completed => 2,
                TripState.Cancelled => 3,
                _ => 4
            }).ThenBy(t => t.Order.TripStartDate).ToList();

            var viewModel = new TripViewModel
            {
                Trips = sortedTrips,
                AssignedTrips = assignedTrips,
                Destination = destination,
                SearchDriverName = searchDriverName,
                VehicleType = vehicleType,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate,
                IsDriver = isDriver,
                IsFleetManager = isFleetManager,
                IsSystemSupport = isSystemSupport,
                CurrentUserId = currentUser.Id
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

            // Check if user is Fleet Manager
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isFleetManager = userRoles.Contains("FleetManager");

            if (!isFleetManager)
            {
                TempData["ErrorMessage"] = "Only Fleet Managers can create trips.";
                return RedirectToAction("Index", "Orders");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Only allow creating trips for approved orders
            if (order.Status != OrderState.Approved)
            {
                TempData["ErrorMessage"] = "Trips can only be created for approved orders.";
                return RedirectToAction("Index", "Orders");
            }

            // Check if a trip already exists for this order
            var existingTrip = await _context.Trips.AnyAsync(t => t.OrderId == order.Id);
            if (existingTrip)
            {
                TempData["ErrorMessage"] = "A trip already exists for this order.";
                return RedirectToAction("Index", "Orders");
            }

            // Get the user who created the order
            var orderUser = await _userManager.FindByIdAsync(userId);
            var createdByUserName = orderUser?.UserName ?? "Unknown User";

            // Pass StartLocation and Destination to the view
            ViewBag.OrderStartLocation = order.StartLocation;
            ViewBag.OrderEndLocation = order.Destination;
            ViewBag.TripStartTime = order.TripStartDate;
            ViewBag.TripEndTime = order.TripEndDate;

            // Get all available vehicles (not filtering by type to show all options)
            var availableVehicles = await _context.Vehicles
                .Where(v => v.Status == VehicleState.available)
                .ToListAsync();

            // Debug: Log the count of available vehicles
            var totalVehicles = await _context.Vehicles.CountAsync();
            var availableCount = availableVehicles.Count;
            
            // Add debug information to ViewBag
            ViewBag.TotalVehicles = totalVehicles;
            ViewBag.AvailableVehiclesCount = availableCount;
            ViewBag.AvailableVehicles = availableVehicles; // For debugging

            // Get all active drivers
            var availableDrivers = await _context.Drivers
                .Where(d => d.DriverStatus == DriverState.active)
                .ToListAsync();

            // Debug: Log the count of available drivers
            var totalDrivers = await _context.Drivers.CountAsync();
            var availableDriversCount = availableDrivers.Count;
            
            // Add debug information to ViewBag
            ViewBag.TotalDrivers = totalDrivers;
            ViewBag.AvailableDriversCount = availableDriversCount;
            ViewBag.AvailableDrivers = availableDrivers; // For debugging

            // Display detailed driver information
            var driverSelectList = availableDrivers.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"{d.UserName} - {d.LicenseNumber} (Active)"
            }).ToList();
            
            ViewBag.DriverId = new SelectList(driverSelectList, "Value", "Text");
            
            // Create custom display format for vehicles (License Plate - Model)
            var vehicleSelectList = availableVehicles.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.LicensePlate} - {v.Model} ({v.Type})"
            }).ToList();
            
            ViewBag.VehicleId = new SelectList(vehicleSelectList, "Value", "Text");
            ViewBag.OrderId = id;
            ViewBag.CreatedBy = userId;
            ViewBag.CreatedByUserName = createdByUserName;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,OrderId,DriverId,CreatedBy")] Trip trip)
        {
            // Debug: Log the received data
            TempData["DebugInfo"] = $"Received: VehicleId={trip.VehicleId}, OrderId={trip.OrderId}, DriverId={trip.DriverId}, CreatedBy={trip.CreatedBy}";
            
            // Check if user is Fleet Manager
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Orders");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isFleetManager = userRoles.Contains("FleetManager");

            if (!isFleetManager)
            {
                TempData["ErrorMessage"] = "Only Fleet Managers can create trips.";
                return RedirectToAction("Index", "Orders");
            }

            // Validate that the order exists and is approved
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == trip.OrderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index", "Orders");
            }

            if (order.Status != OrderState.Approved)
            {
                TempData["ErrorMessage"] = "Trips can only be created for approved orders.";
                return RedirectToAction("Index", "Orders");
            }

            // Check if a trip already exists for this order
            var existingTrip = await _context.Trips.AnyAsync(t => t.OrderId == trip.OrderId);
            if (existingTrip)
            {
                ModelState.AddModelError("OrderId", "A trip already exists for this order.");
            }

            // Validate that the vehicle exists and is available
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == trip.VehicleId);
            if (vehicle == null)
            {
                ModelState.AddModelError("VehicleId", "Selected vehicle not found.");
            }
            else if (vehicle.Status != VehicleState.available)
            {
                ModelState.AddModelError("VehicleId", "Selected vehicle is not available.");
            }

            // Validate that the driver exists and is active
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId);
            if (driver == null)
            {
                ModelState.AddModelError("DriverId", "Selected driver not found.");
            }
            else if (driver.DriverStatus != DriverState.active)
            {
                ModelState.AddModelError("DriverId", "Selected driver is not active.");
            }

            // Clear navigation property errors since we're only binding IDs
            ModelState.Remove("Order");
            ModelState.Remove("Driver");
            ModelState.Remove("Vehicle");
            ModelState.Remove("CreatedByUser");

            // Debug: Log ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["DebugInfo"] += $"; ModelState Errors: {errors}";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    trip.CreatedAt = DateTime.Now;
                    trip.Distance = 0; // Initialize distance to 0, will be calculated automatically
                    trip.Status = TripState.Scheduled; // Automatically set status to Scheduled
                    _context.Add(trip);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Trip created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating trip: {ex.Message}";
                    // Continue to reload the form with error
                }
            }

            // Reload data for the form (in case of validation errors)
            var orderUser = await _userManager.FindByIdAsync(trip.CreatedBy);
            var createdByUserName = orderUser?.UserName ?? "Unknown User";
            
            // Reload available vehicles and drivers
            var availableVehicles = await _context.Vehicles
                .Where(v => v.Status == VehicleState.available)
                .ToListAsync();
            
            var availableDrivers = await _context.Drivers
                .Where(d => d.DriverStatus == DriverState.active)
                .ToListAsync();
            
            // Create detailed driver display format
            var driverSelectList = availableDrivers.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"{d.UserName} - {d.LicenseNumber} (Active)"
            }).ToList();
            
            ViewBag.DriverId = new SelectList(driverSelectList, "Value", "Text", trip.DriverId);
            
            // Create custom display format for vehicles (License Plate - Model)
            var vehicleSelectList = availableVehicles.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.LicensePlate} - {v.Model} ({v.Type})"
            }).ToList();
            
            ViewBag.VehicleId = new SelectList(vehicleSelectList, "Value", "Text", trip.VehicleId);
            ViewBag.OrderId = trip.OrderId;
            ViewBag.CreatedBy = trip.CreatedBy;
            ViewBag.CreatedByUserName = createdByUserName;
            ViewBag.OrderStartLocation = order.StartLocation;
            ViewBag.OrderEndLocation = order.Destination;
            ViewBag.TripStartTime = order.TripStartDate;
            ViewBag.TripEndTime = order.TripEndDate;
            
            // Add debug information
            ViewBag.TotalVehicles = availableVehicles.Count;
            ViewBag.AvailableVehiclesCount = availableVehicles.Count;
            ViewBag.TotalDrivers = availableDrivers.Count;
            ViewBag.AvailableDriversCount = availableDrivers.Count;
            
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
