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
using SmartFleet.Services.Implemenations;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly ITripStateManagementService _tripStateService;
        private readonly IDriverStatusManagementService _driverStatusService;
        private readonly IVehicleStateManagementService _vehicleStateService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;
        private readonly IPaginationService _paginationService;
        private readonly ISearchService _searchService;

        public TripsController(SmartFleetContext context, ITripStateManagementService tripStateService, 
            IDriverStatusManagementService driverStatusService, IVehicleStateManagementService vehicleStateService,
            UserManager<ApplicationUser> userManager, INotificationService notificationService, IUserRoleService userRoleService, IPaginationService paginationService, ISearchService searchService)
        {
            _context = context;
            _tripStateService = tripStateService;
            _driverStatusService = driverStatusService;
            _vehicleStateService = vehicleStateService;
            _userManager = userManager;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
            _paginationService = paginationService;
            _searchService = searchService;
        }

        /// <summary>
        /// Gets available vehicles for trip assignment, excluding those with conflicting states or intersecting trips
        /// </summary>
        private async Task<List<Vehicle>> GetAvailableVehiclesAsync(DateTime? tripStartDate = null, DateTime? tripEndDate = null, int? currentVehicleId = null)
        {
            var query = _context.Vehicles.AsQueryable();
            
            // Exclude vehicles in unavailable states
            query = query.Where(v => v.Status != VehicleState.on_trip && 
                                     v.Status != VehicleState.need_maintenance && 
                                     v.Status != VehicleState.under_maintenance && 
                                     v.Status != VehicleState.maintained);
            
            // If we have trip dates, exclude vehicles with intersecting scheduled trips
            if (tripStartDate.HasValue && tripEndDate.HasValue)
            {
                var conflictingVehicleIds = await _context.Trips
                    .Include(t => t.Order)
                    .Where(t => t.Status == TripState.Scheduled &&
                               t.VehicleId != currentVehicleId && // Allow current vehicle in edit mode
                               ((t.Order.TripStartDate <= tripStartDate && t.Order.TripEndDate > tripStartDate) ||
                                (t.Order.TripStartDate < tripEndDate && t.Order.TripEndDate >= tripEndDate) ||
                                (t.Order.TripStartDate >= tripStartDate && t.Order.TripEndDate <= tripEndDate)))
                    .Select(t => t.VehicleId)
                    .Distinct()
                    .ToListAsync();
                
                if (conflictingVehicleIds.Any())
                {
                    query = query.Where(v => !conflictingVehicleIds.Contains(v.Id));
                }
            }
            
            return await query.ToListAsync();
        }

        /// <summary>
        /// Gets available drivers for trip assignment, excluding unavailable drivers and those with intersecting trips
        /// </summary>
        private async Task<List<Driver>> GetAvailableDriversAsync(DateTime? tripStartDate = null, DateTime? tripEndDate = null, string? currentDriverId = null)
        {
            var query = _context.Drivers.AsQueryable();
            
            // Exclude unavailable drivers
            query = query.Where(d => d.DriverStatus != DriverState.NotAvailable);
            
            // If we have trip dates, exclude drivers with intersecting scheduled trips
            if (tripStartDate.HasValue && tripEndDate.HasValue)
            {
                var conflictingDriverIds = await _context.Trips
                    .Include(t => t.Order)
                    .Where(t => t.Status == TripState.Scheduled &&
                               t.DriverId != currentDriverId && // Allow current driver in edit mode
                               ((t.Order.TripStartDate <= tripStartDate && t.Order.TripEndDate > tripStartDate) ||
                                (t.Order.TripStartDate < tripEndDate && t.Order.TripEndDate >= tripEndDate) ||
                                (t.Order.TripStartDate >= tripStartDate && t.Order.TripEndDate <= tripEndDate)))
                    .Select(t => t.DriverId)
                    .Distinct()
                    .ToListAsync();
                
                if (conflictingDriverIds.Any())
                {
                    query = query.Where(d => !conflictingDriverIds.Contains(d.Id));
                }
            }
            
            return await query.ToListAsync();
        }

        public async Task<IActionResult> Index(
            string destination, string searchDriverName, VehicleType? vehicleType, TripState? stateFilter, DateTime? startDate, DateTime? endDate,
            string assignedDestination, string assignedSearchDriverName, VehicleType? assignedVehicleType, TripState? assignedStateFilter, DateTime? assignedStartDate, DateTime? assignedEndDate,
            int pageNumber = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isDriver = userRoles.Contains("Driver");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSystemSupport = userRoles.Contains("SysSupport");
            var isNormalUser = userRoles.Contains("NormalUser");
            var isCommissioner = userRoles.Contains("commissioner");
            var isMaintenanceManager = userRoles.Contains("MaintenanceManager");
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
                tripsQuery = tripsQuery.Where(t => t.DriverId == currentUser.Id);
                
                // Populate assigned trips for drivers
                var assignedTripsQuery = _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .Include(t => t.Order)
                    .Include(t => t.CreatedByUser)
                    .Where(t => t.DriverId == currentUser.Id);
                
                // Apply assigned trips filters
                var assignedFilters = new List<System.Linq.Expressions.Expression<Func<Trip, bool>>>();
                if (!string.IsNullOrEmpty(assignedDestination))
                    assignedFilters.Add(t => t.Order.Destination.Contains(assignedDestination));
                if (!string.IsNullOrEmpty(assignedSearchDriverName))
                    assignedFilters.Add(t => t.Driver.UserName.Contains(assignedSearchDriverName));
                if (assignedVehicleType.HasValue)
                    assignedFilters.Add(t => t.Vehicle.Type == assignedVehicleType.Value);
                if (assignedStateFilter.HasValue)
                    assignedFilters.Add(t => t.Status == assignedStateFilter.Value);
                if (assignedStartDate.HasValue)
                    assignedFilters.Add(t => t.Order.TripStartDate >= assignedStartDate.Value);
                if (assignedEndDate.HasValue)
                    assignedFilters.Add(t => t.Order.TripEndDate <= assignedEndDate.Value);
                
                assignedTripsQuery = _searchService.ApplyFilters(assignedTripsQuery, assignedFilters);
                assignedTrips = await assignedTripsQuery
                    .OrderBy(t => t.Status)
                    .ThenBy(t => t.Order.TripStartDate)
                    .ToListAsync();
            }
            else if (isNormalUser)
            {
                tripsQuery = tripsQuery.Where(t => t.Order.UserId == currentUser.Id);
            }
            else if (isCommissioner)
            {
                TempData["ErrorMessage"] = "You don't have access to trips.";
                return RedirectToAction("Index", "Home");
            }
            else if (isMaintenanceManager)
            {
                TempData["ErrorMessage"] = "You don't have access to trips.";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                tripsQuery = tripsQuery.Where(t => t.Order.UserId == currentUser.Id);
            }
            var filters = new List<System.Linq.Expressions.Expression<Func<Trip, bool>>>();
            if (!string.IsNullOrEmpty(destination))
                filters.Add(t => t.Order.Destination.Contains(destination));
            if (!string.IsNullOrEmpty(searchDriverName))
                filters.Add(t => t.Driver.UserName.Contains(searchDriverName));
            if (vehicleType.HasValue)
                filters.Add(t => t.Vehicle.Type == vehicleType.Value);
            if (stateFilter.HasValue)
                filters.Add(t => t.Status == stateFilter.Value);
            if (startDate.HasValue)
                filters.Add(t => t.Order.TripStartDate >= startDate.Value);
            if (endDate.HasValue)
                filters.Add(t => t.Order.TripEndDate <= endDate.Value);
            tripsQuery = _searchService.ApplyFilters(tripsQuery, filters);
            int pageSize = 10;
            int totalCount = await tripsQuery.CountAsync();
            var pagedTrips = await _paginationService.GetPaginatedAsync(tripsQuery.OrderBy(t => t.Status).ThenBy(t => t.Order.TripStartDate), pageNumber, pageSize);

            var viewModel = new TripViewModel
            {
                Trips = pagedTrips,
                AssignedTrips = assignedTrips,
                Destination = destination,
                SearchDriverName = searchDriverName,
                VehicleType = vehicleType,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate,
                AssignedDestination = assignedDestination,
                AssignedSearchDriverName = assignedSearchDriverName,
                AssignedVehicleType = assignedVehicleType,
                AssignedStateFilter = assignedStateFilter,
                AssignedStartDate = assignedStartDate,
                AssignedEndDate = assignedEndDate,
                IsDriver = isDriver,
                IsFleetManager = isFleetManager,
                IsSysSupport = isSystemSupport,
                IsNormalUser = isNormalUser,
                CurrentUserId = currentUser.Id
            };

            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.Destination = destination;
            ViewBag.SearchDriverName = searchDriverName;
            ViewBag.VehicleType = vehicleType;
            ViewBag.StateFilter = stateFilter;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(viewModel);
        }

        // GET: Trips/Details/5
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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isDriver = userRoles.Contains("Driver");
            var isNormalUser = userRoles.Contains("NormalUser");
            var isMaintenanceManager = userRoles.Contains("MaintenanceManager");

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

            // Check if user has permission to view this trip
            if (isMaintenanceManager)
            {
                TempData["ErrorMessage"] = "Maintenance managers don't have access to trips.";
                return RedirectToAction("Index", "Home");
            }
            else if (isDriver && trip.DriverId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only view trips assigned to you.";
                return RedirectToAction(nameof(Index));
            }
            else if (isNormalUser && trip.Order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only view trips from your own orders.";
                return RedirectToAction(nameof(Index));
            }

            return View(trip);
        }

        // GET: Trips/Create
        public async Task<IActionResult> Create(int? id, string? userId)
        {
            if (id == null || string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can create trips
            if (!await _userRoleService.CanCreateTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create trips.";
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
            var availableVehicles = await GetAvailableVehiclesAsync(order.TripStartDate, order.TripEndDate);
            
            var availableDrivers = await GetAvailableDriversAsync(order.TripStartDate, order.TripEndDate, null);
            
            // Create detailed driver display format
            var driverSelectList = availableDrivers.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"{d.UserName} - {d.LicenseNumber} ({d.DriverStatus})"
            }).ToList();
            
            ViewBag.DriverId = new SelectList(driverSelectList, "Value", "Text");
            
            // Create custom display format for vehicles (License Plate - Model)
            var vehicleSelectList = availableVehicles.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.LicensePlate} - {v.Model} ({v.Type}) - {v.Status}"
            }).ToList();
            
            ViewBag.VehicleId = new SelectList(vehicleSelectList, "Value", "Text");
            ViewBag.OrderId = order.Id;
            ViewBag.CreatedBy = userId;
            ViewBag.CreatedByUserName = createdByUserName;
            
            // Add debug information
            ViewBag.TotalVehicles = availableVehicles.Count;
            ViewBag.AvailableVehiclesCount = availableVehicles.Count;
            ViewBag.TotalDrivers = availableDrivers.Count;
            ViewBag.AvailableDriversCount = availableDrivers.Count;
            
            // Calculate and add resource availability
            ViewBag.ResourceAvailability = await GetOrderResourceAvailabilityAsync(order);
            
            return View();
        }

        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,OrderId,DriverId,CreatedBy")] Trip trip)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Orders");
            }

            // Check if user can create trips
            if (!await _userRoleService.CanCreateTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create trips.";
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
            else if (vehicle.Status != VehicleState.available && vehicle.Status != VehicleState.maintained)
            {
                ModelState.AddModelError("VehicleId", "Selected vehicle is not available for trips.");
            }

            // Validate that the driver exists and is active
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId);
            if (driver == null)
            {
                ModelState.AddModelError("DriverId", "Selected driver not found.");
            }
            else if (driver.DriverStatus != DriverState.Available)
            {
                ModelState.AddModelError("DriverId", "Selected driver is not available.");
            }

            // Clear navigation property errors since we're only binding IDs
            ModelState.Remove("Order");
            ModelState.Remove("Driver");
            ModelState.Remove("Vehicle");
            ModelState.Remove("CreatedByUser");

            if (ModelState.IsValid)
            {
                try
                {
                    trip.CreatedAt = DateTime.Now;
                    trip.Distance = 0; // Initialize distance to 0, will be calculated automatically
                    trip.Status = TripState.Scheduled; // Automatically set status to Scheduled
                    _context.Add(trip);
                    await _context.SaveChangesAsync();
                    
                    // Update driver status to OnTrip
                    await _driverStatusService.UpdateDriverStatusOnTripAssignmentAsync(trip.DriverId);
                    
                    // Update vehicle state to on_scheduled_trip
                    await _vehicleStateService.UpdateVehicleStateOnTripAssignmentAsync(trip.VehicleId);
                    
                    // After trip is created and saved in POST Create
                    await _notificationService.CreateNotificationAsync(
                        trip.DriverId,
                        "Trip Assigned",
                        $"You have been assigned to a new trip (ID: {trip.Id}) from {order.StartLocation} to {order.Destination}. Trip starts at {order.TripStartDate:yyyy-MM-dd HH:mm}.",
                        RelatedTable.Trip,
                        trip.Id
                    );
                    
                    // Send notification to the order creator
                    await _notificationService.CreateNotificationAsync(
                        order.UserId,
                        "Trip Created",
                        $"A trip (ID: {trip.Id}) has been created for your order from {order.StartLocation} to {order.Destination}. Trip starts at {order.TripStartDate:yyyy-MM-dd HH:mm}.",
                        RelatedTable.Trip,
                        trip.Id
                    );
                    
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
            var availableVehicles = await GetAvailableVehiclesAsync(order.TripStartDate, order.TripEndDate);
            
            var availableDrivers = await GetAvailableDriversAsync(order.TripStartDate, order.TripEndDate, null);
            
            // Create detailed driver display format
            var driverSelectList = availableDrivers.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"{d.UserName} - {d.LicenseNumber} ({d.DriverStatus})"
            }).ToList();
            
            ViewBag.DriverId = new SelectList(driverSelectList, "Value", "Text", trip.DriverId);
            
            // Create custom display format for vehicles (License Plate - Model)
            var vehicleSelectList = availableVehicles.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.LicensePlate} - {v.Model} ({v.Type}) - {v.Status}"
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
            
            // Calculate and add resource availability
            ViewBag.ResourceAvailability = await GetOrderResourceAvailabilityAsync(order);
            
            return View(trip);
        }

        // GET: Trips/Edit/5
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

            // Check if user can edit trips
            if (!await _userRoleService.CanEditTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit trips.";
                return RedirectToAction(nameof(Index));
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

            var drivers = await GetAvailableDriversAsync(trip.Order.TripStartDate, trip.Order.TripEndDate, trip.DriverId);
            var vehicles = await GetAvailableVehiclesAsync(trip.Order.TripStartDate, trip.Order.TripEndDate, trip.VehicleId);

            ViewData["VehicleId"] = new SelectList(vehicles, "Id", "LicensePlate", trip.VehicleId);
            var driverSelectList = drivers.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"{d.UserName} - {d.LicenseNumber} ({d.DriverStatus})"
            }).ToList();
            ViewData["DriverId"] = new SelectList(driverSelectList, "Value", "Text", trip.DriverId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Id", trip.CreatedBy);
            ViewBag.VehicleGeofence = trip.Vehicle?.Geofence;
            ViewBag.CreatedByUserName = trip.Order?.User?.UserName ?? "Unknown User";
            ViewBag.TripStartTime = trip.Order?.TripStartDate;
            ViewBag.TripEndTime = trip.Order?.TripEndDate;
            ViewBag.OrderStartLocation = trip.Order?.StartLocation;
            ViewBag.OrderEndLocation = trip.Order?.Destination;
            ViewBag.TripStatus = trip.Status;
            ViewBag.OrderId = trip.OrderId;
            return View(trip);
        }

        // POST: Trips/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,DriverId,Status,CreatedAt,CreatedBy")] Trip trip)
        {
            if (id != trip.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can edit trips
            if (!await _userRoleService.CanEditTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit trips.";
                return RedirectToAction(nameof(Index));
            }

            // Before if (ModelState.IsValid) in Edit POST action:
            ModelState.Remove("Order");
            ModelState.Remove("Vehicle");
            ModelState.Remove("Driver");
            ModelState.Remove("CreatedByUser");

            // Validate that the vehicle exists
            var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == trip.VehicleId);
            if (!vehicleExists)
            {
                ModelState.AddModelError("VehicleId", "Selected vehicle not found.");
            }

            // Validate that the driver exists
            var driverExists = await _context.Drivers.AnyAsync(d => d.Id == trip.DriverId);
            if (!driverExists)
            {
                ModelState.AddModelError("DriverId", "Selected driver not found.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalTrip = await _context.Trips
                        .Include(t => t.Driver)
                        .Include(t => t.Vehicle)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (originalTrip == null)
                    {
                        return NotFound();
                    }

                    // Store original values for status updates
                    var originalDriverId = originalTrip.DriverId;
                    var originalVehicleId = originalTrip.VehicleId;
                    var originalStatus = originalTrip.Status;

                    // Update trip properties
                    originalTrip.VehicleId = trip.VehicleId;
                    originalTrip.DriverId = trip.DriverId;
                    originalTrip.Status = trip.Status;

                    _context.Update(originalTrip);
                    await _context.SaveChangesAsync();

                    // Update driver status if driver changed
                    if (originalDriverId != trip.DriverId)
                    {
                        // Reset original driver status
                        await _driverStatusService.UpdateDriverStatusOnTripCompletionAsync(originalDriverId);
                        // Set new driver status
                        await _driverStatusService.UpdateDriverStatusOnTripAssignmentAsync(trip.DriverId);
                    }

                    // Update vehicle state if vehicle changed
                    if (originalVehicleId != trip.VehicleId)
                    {
                        // Reset original vehicle state
                        await _vehicleStateService.UpdateVehicleStateOnTripCompletionAsync(originalVehicleId);
                        // Set new vehicle state
                        await _vehicleStateService.UpdateVehicleStateOnTripAssignmentAsync(trip.VehicleId);
                    }

                    // Send notification to the new driver if driver changed
                    if (originalDriverId != trip.DriverId)
                    {
                        await _notificationService.CreateNotificationAsync(
                            trip.DriverId,
                            "Trip Assigned",
                            $"You have been assigned to trip (ID: {trip.Id}).",
                            RelatedTable.Trip,
                            trip.Id
                        );
                    }

                    // Send notification to the order creator about the edit
                    var order = await _context.Orders.FindAsync(originalTrip.OrderId);
                    if (order != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            order.UserId,
                            "Trip Updated",
                            $"Your trip (ID: {trip.Id}) has been updated.",
                            RelatedTable.Trip,
                            trip.Id
                        );
                    }

                    TempData["SuccessMessage"] = "Trip updated successfully.";
                    return RedirectToAction(nameof(Index));
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
            }

            // Reload data for the form (in case of validation errors)
            var reloadedTrip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.Vehicle)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reloadedTrip == null)
            {
                return NotFound();
            }

            var drivers = await GetAvailableDriversAsync(reloadedTrip.Order.TripStartDate, reloadedTrip.Order.TripEndDate, reloadedTrip.DriverId);
            var vehicles = await GetAvailableVehiclesAsync(reloadedTrip.Order.TripStartDate, reloadedTrip.Order.TripEndDate, reloadedTrip.VehicleId);

            ViewData["VehicleId"] = new SelectList(vehicles, "Id", "LicensePlate", trip.VehicleId);
            var driverSelectList = drivers.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"{d.UserName} - {d.LicenseNumber} ({d.DriverStatus})"
            }).ToList();
            ViewData["DriverId"] = new SelectList(driverSelectList, "Value", "Text", trip.DriverId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Id", trip.CreatedBy);
            ViewBag.VehicleGeofence = reloadedTrip.Vehicle?.Geofence;
            ViewBag.CreatedByUserName = reloadedTrip.Order?.User?.UserName ?? "Unknown User";
            ViewBag.TripStartTime = reloadedTrip.Order?.TripStartDate;
            ViewBag.TripEndTime = reloadedTrip.Order?.TripEndDate;
            ViewBag.OrderStartLocation = reloadedTrip.Order?.StartLocation;
            ViewBag.OrderEndLocation = reloadedTrip.Order?.Destination;
            ViewBag.TripStatus = reloadedTrip.Status;
            ViewBag.OrderId = reloadedTrip.OrderId;
            return View(trip);
        }

        // GET: Trips/Delete/5
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

            // Check if user can edit trips (only FleetManager and SysSupport can delete trips)
            if (!await _userRoleService.CanEditTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to delete trips.";
                return RedirectToAction(nameof(Index));
            }

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.Vehicle)
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
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can edit trips (only FleetManager and SysSupport can delete trips)
            if (!await _userRoleService.CanEditTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to delete trips.";
                return RedirectToAction(nameof(Index));
            }

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (trip != null)
            {
                var driverId = trip.DriverId;
                var vehicleId = trip.VehicleId;
                var order = trip.Order;
                
                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();
                
                // Update driver status after trip deletion
                await _driverStatusService.UpdateDriverStatusOnTripCompletionAsync(driverId);
                
                // Update vehicle state after trip deletion
                await _vehicleStateService.UpdateVehicleStateOnTripCompletionAsync(vehicleId);

                // Send notification to the order creator
                if (order != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        order.UserId,
                        "Trip Deleted",
                        $"The trip (ID: {id}) for your order from {order.StartLocation} to {order.Destination} has been deleted.",
                        RelatedTable.Trip,
                        id
                    );
                }
            }

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
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can edit trips (only FleetManager and SysSupport can recalculate distance)
            if (!await _userRoleService.CanEditTrip(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to recalculate trip distance.";
                return RedirectToAction(nameof(Index));
            }

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
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (trip == null)
            {
                return NotFound();
            }

            // Check if user can cancel this trip
            if (!await _userRoleService.CanCancelTrip(currentUser, trip.Status))
            {
                TempData["ErrorMessage"] = "You don't have permission to cancel this trip.";
                return RedirectToAction(nameof(Details), new { id = trip.Id });
            }

            // For NormalUser - only allow cancelling trips from their own orders
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isNormalUser = userRoles.Contains("NormalUser");
            if (isNormalUser && trip.Order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only cancel trips from your own orders.";
                return RedirectToAction(nameof(Details), new { id = trip.Id });
            }

            trip.Status = TripState.Cancelled;
            await _context.SaveChangesAsync();
            
            // Update driver status after trip cancellation
            await _driverStatusService.UpdateDriverStatusOnTripCompletionAsync(trip.DriverId);
            
            // Update vehicle state after trip cancellation
            await _vehicleStateService.UpdateVehicleStateOnTripCompletionAsync(trip.VehicleId);

            // Send notification to FleetManager
            var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
            var notificationTitle = $"Trip Ended";
            var notificationMessage = $"Trip (ID: {trip.Id}) has ended (Status: Cancelled). The attached vehicle and driver are now free.";
            foreach (var user in fleetManagers)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    notificationTitle,
                    notificationMessage,
                    RelatedTable.Trip,
                    trip.Id
                );
            }

            // In Cancel action, after trip.Status = TripState.Cancelled and SaveChangesAsync()
            await _notificationService.CreateNotificationAsync(
                trip.DriverId,
                "Trip Cancelled",
                $"Your assigned trip (ID: {trip.Id}) scheduled from {trip.Order.StartLocation} to {trip.Order.Destination} at {trip.Order.TripStartDate:yyyy-MM-dd HH:mm} has been cancelled.",
                RelatedTable.Trip,
                trip.Id
            );

            // Send notification to the order creator
            await _notificationService.CreateNotificationAsync(
                trip.Order.UserId,
                "Trip Cancelled",
                $"Your trip (ID: {trip.Id}) from {trip.Order.StartLocation} to {trip.Order.Destination} has been cancelled.",
                RelatedTable.Trip,
                trip.Id
            );

            TempData["SuccessMessage"] = "Trip cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Helper to check resource availability for an order
        private async Task<string> GetOrderResourceAvailabilityAsync(Order order)
        {
            // Check available vehicles (match type and capacity)
            var vehicles = await _context.Vehicles.Where(v => v.Status != VehicleState.on_trip &&
                                                             v.Status != VehicleState.need_maintenance &&
                                                             v.Status != VehicleState.under_maintenance &&
                                                             v.Status != VehicleState.maintained &&
                                                             v.Type == order.VehicleType &&
                                                             v.Capacity >= order.PassengerCount).ToListAsync();
            var conflictingVehicleIds = await _context.Trips
                .Include(t => t.Order)
                .Where(t => t.Status == TripState.Scheduled &&
                           ((t.Order.TripStartDate <= order.TripStartDate && t.Order.TripEndDate > order.TripStartDate) ||
                            (t.Order.TripStartDate < order.TripEndDate && t.Order.TripEndDate >= order.TripEndDate) ||
                            (t.Order.TripStartDate >= order.TripStartDate && t.Order.TripEndDate <= order.TripEndDate)))
                .Select(t => t.VehicleId)
                .Distinct()
                .ToListAsync();
            var availableVehicles = vehicles.Where(v => !conflictingVehicleIds.Contains(v.Id)).ToList();

            // Check available drivers
            var drivers = await _context.Drivers.Where(d => d.DriverStatus != DriverState.NotAvailable).ToListAsync();
            var conflictingDriverIds = await _context.Trips
                .Include(t => t.Order)
                .Where(t => t.Status == TripState.Scheduled &&
                           ((t.Order.TripStartDate <= order.TripStartDate && t.Order.TripEndDate > order.TripStartDate) ||
                            (t.Order.TripStartDate < order.TripEndDate && t.Order.TripEndDate >= order.TripEndDate) ||
                            (t.Order.TripStartDate >= order.TripStartDate && t.Order.TripEndDate <= order.TripEndDate)))
                .Select(t => t.DriverId)
                .Distinct()
                .ToListAsync();
            var availableDrivers = drivers.Where(d => !conflictingDriverIds.Contains(d.Id)).ToList();

            if (availableVehicles.Any() && availableDrivers.Any())
                return "Available";
            else
                return "Not Available";
        }
    }
}
