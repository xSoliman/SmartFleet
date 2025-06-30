using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;
        private readonly IPaginationService _paginationService;
        private readonly ISearchService _searchService;
        private readonly IReferenceCheckService _referenceCheckService;

        public OrdersController(SmartFleetContext context, UserManager<ApplicationUser> userManager, 
            INotificationService notificationService, IUserRoleService userRoleService, IPaginationService paginationService, ISearchService searchService, IReferenceCheckService referenceCheckService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
            _paginationService = paginationService;
            _searchService = searchService;
            _referenceCheckService = referenceCheckService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string searchKeyword, OrderState? stateFilter, DateTime? startDate, DateTime? endDate, int pageNumber = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminUser = userRoles.Contains("SysSupport") || userRoles.Contains("FleetManager");
            var isCommissioner = userRoles.Contains("commissioner");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");
            var isNormalUser = userRoles.Contains("NormalUser");

            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Trip)
                .AsQueryable();

            // Apply role-based filtering
            if (isNormalUser)
            {
                ordersQuery = ordersQuery.Where(o => o.UserId == currentUser.Id);
            }
            // Commissioner can see all orders (no filtering)

            // Apply unified search
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                var searchTerm = searchKeyword.ToLower();
                ordersQuery = ordersQuery.Where(o =>
                    o.User.UserName.ToLower().Contains(searchTerm) ||
                    o.StartLocation.ToLower().Contains(searchTerm) ||
                    o.Destination.ToLower().Contains(searchTerm) ||
                    o.Reason.ToLower().Contains(searchTerm)
                );
            }

            // Apply filters
            if (stateFilter.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.Status == stateFilter.Value);
            }
            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.TripStartDate.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.TripEndDate.Date <= endDate.Value.Date);
            }

            // Calculate resource availability for pending orders
            var resourceAvailability = new Dictionary<int, string>();
            if (isFleetManager || isCommissioner)
            {
                var pendingOrders = await ordersQuery
                    .Where(o => o.Status == OrderState.Pending)
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    var hasResources = await CheckResourceAvailabilityAsync(order);
                    resourceAvailability[order.Id] = hasResources ? "Available" : "Not Available";
            }
            }

            // Apply pagination
            int pageSize = 10;
            var orders = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalOrders = await ordersQuery.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;

            var viewModel = new OrderViewModel
            {
                Orders = orders,
                IsAdminUser = isAdminUser,
                IsCommissioner = isCommissioner,
                IsFleetManager = isFleetManager,
                IsSysSupport = isSysSupport,
                IsNormalUser = isNormalUser,
                SearchKeyword = searchKeyword,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate,
                ResourceAvailability = resourceAvailability,
                CanCreateOrder = await _userRoleService.CanCreateOrder(currentUser),
                CurrentUserId = currentUser.Id
            };

            return View(viewModel);
        }

        // GET: Orders/Details/5
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
            var isCommissioner = userRoles.Contains("commissioner");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");
            var isDriver = userRoles.Contains("Driver");
            var isMaintenanceManager = userRoles.Contains("MaintenanceManager");
            var isNormalUser = userRoles.Contains("NormalUser");

            // Check access permissions
            if (!await _userRoleService.HasAccessToOrders(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have access to orders.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Include Trip for FleetManager and Commissioner to check if trip exists
            if (isFleetManager || isCommissioner)
            {
                order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Trip)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }

            // Check if user has permission to view this order
            if (isDriver)
            {
                TempData["ErrorMessage"] = "Drivers don't have access to orders.";
                return RedirectToAction("Index", "Home");
            }
            else if (isMaintenanceManager)
            {
                TempData["ErrorMessage"] = "Maintenance managers don't have access to orders.";
                return RedirectToAction("Index", "Home");
            }
            else if (isNormalUser && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only view your own orders.";
                return RedirectToAction(nameof(Index));
            }
            // Commissioner can view details for all orders (no additional check needed)

            ViewBag.IsCommissioner = isCommissioner;
            ViewBag.IsFleetManager = isFleetManager;
            ViewBag.IsSysSupport = isSysSupport;
            ViewBag.IsAdminUser = isFleetManager || isSysSupport || isCommissioner;

            // For commissioner and fleet manager, add resource availability
            if ((isCommissioner || isFleetManager) && order != null)
            {
                ViewBag.ResourceAvailability = await GetOrderResourceAvailabilityAsync(order);
            }

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can create orders
            if (!await _userRoleService.CanCreateOrder(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create orders.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.VehicleTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Car", Text = "Car" },
                new SelectListItem { Value = "Truck", Text = "Truck" },
                new SelectListItem { Value = "Bus", Text = "Bus" },
                new SelectListItem { Value = "Van", Text = "Van" },
                new SelectListItem { Value = "Motorcycle", Text = "Motorcycle" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleType,PassengerCount,StartLocation,Destination,TripStartDate,TripEndDate,Reason,CreatedAt")] Order order)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can create orders
            if (!await _userRoleService.CanCreateOrder(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to create orders.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderState.Pending; 
            order.UserId = currentUser.Id;

            _context.Add(order);
            await _context.SaveChangesAsync();

            // Send notifications to SysSupport, FleetManager, and Commissioner
            await SendOrderCreationNotifications(order, currentUser);

            CookieOptions option = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddHours(1), 
                HttpOnly = true, 
                Secure = true
            };

            Response.Cookies.Append("OrderId", order.Id.ToString(), option);

            return RedirectToAction(nameof(Index));
        }

        // Private method to send notifications for order creation
        private async Task SendOrderCreationNotifications(Order order, ApplicationUser orderCreator)
        {
            try
            {
                // Get users with SysSupport role
                
                // Get users with FleetManager role
                var fleetManagerUsers = await _userRoleService.GetUsersByRole("FleetManager");
                
                // Get users with commissioner role
                var commissionerUsers = await _userRoleService.GetUsersByRole("commissioner");

                // Combine all users who should receive notifications
                var allNotificationUsers = new List<ApplicationUser>();
                allNotificationUsers.AddRange(fleetManagerUsers);
                allNotificationUsers.AddRange(commissionerUsers);

                // Remove duplicates (in case a user has multiple roles)
                var uniqueUsers = allNotificationUsers.GroupBy(u => u.Id).Select(g => g.First()).ToList();

                // Create notification message
                var notificationTitle = "New Order Created";
                var notificationMessage = $"User {orderCreator.UserName} has created a new order (ID: {order.Id}) for {order.VehicleType} from {order.StartLocation} to {order.Destination}.";

                // Send notification to each user
                foreach (var user in uniqueUsers)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        notificationTitle,
                        notificationMessage,
                        RelatedTable.Order,
                        order.Id
                    );
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the order creation
                // In a production environment, you might want to use a proper logging service
                Console.WriteLine($"Error sending order creation notifications: {ex.Message}");
            }
        }

        // GET: Orders/Edit/5
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

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user can edit this order
            if (!await _userRoleService.CanEditOrder(currentUser, order.Status))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this order.";
                return RedirectToAction(nameof(Index));
            }

            // For NormalUser - only allow editing their own orders
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isNormalUser = userRoles.Contains("NormalUser");
            if (isNormalUser && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only edit your own orders.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", order.UserId);
            ViewBag.VehicleTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = VehicleType.Car.ToString(), Text = "Car" },
                new SelectListItem { Value = VehicleType.Truck.ToString(), Text = "Truck" },
                new SelectListItem { Value = VehicleType.Bus.ToString(), Text = "Bus" },
                new SelectListItem { Value = VehicleType.Van.ToString(), Text = "Van" },
                new SelectListItem { Value = VehicleType.Motorcycle.ToString(), Text = "Motorcycle" },
                new SelectListItem { Value = VehicleType.Other.ToString(), Text = "Other" }
            };
            ViewBag.UserName = order.User?.UserName;
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,VehicleType,PassengerCount,StartLocation,Destination,TripStartDate,TripEndDate,Reason,Status,CreatedAt")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can edit this order
            if (!await _userRoleService.CanEditOrder(currentUser, order.Status))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this order.";
                return RedirectToAction(nameof(Index));
            }

            // For NormalUser - only allow editing their own orders
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isNormalUser = userRoles.Contains("NormalUser");
            if (isNormalUser && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only edit your own orders.";
                return RedirectToAction(nameof(Index));
            }

            // Remove validation errors for navigation properties
            ModelState.Remove("User");
            ModelState.Remove("Trip");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _context.Orders
                        .Include(o => o.User)
                        .Include(o => o.Trip)
                        .FirstOrDefaultAsync(o => o.Id == id);

                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    // Update only the editable fields
                    existingOrder.VehicleType = order.VehicleType;
                    existingOrder.PassengerCount = order.PassengerCount;
                    existingOrder.StartLocation = order.StartLocation;
                    existingOrder.Destination = order.Destination;
                    existingOrder.TripStartDate = order.TripStartDate;
                    existingOrder.TripEndDate = order.TripEndDate;
                    existingOrder.Reason = order.Reason;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
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

            // If we got this far, something failed, redisplay form
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", order.UserId);
            ViewBag.VehicleTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = VehicleType.Car.ToString(), Text = "Car" },
                new SelectListItem { Value = VehicleType.Truck.ToString(), Text = "Truck" },
                new SelectListItem { Value = VehicleType.Bus.ToString(), Text = "Bus" },
                new SelectListItem { Value = VehicleType.Van.ToString(), Text = "Van" },
                new SelectListItem { Value = VehicleType.Motorcycle.ToString(), Text = "Motorcycle" },
                new SelectListItem { Value = VehicleType.Other.ToString(), Text = "Other" }
            };

            var user = await _context.Users.FindAsync(order.UserId);
            ViewBag.UserName = user?.UserName;
            return View(order);
        }

        // GET: Orders/Delete/5
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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isNormalUser = userRoles.Contains("NormalUser");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to delete this order
            if (isNormalUser && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only delete your own orders.";
                return RedirectToAction(nameof(Index));
            }

            // Only allow deletion of pending orders
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isNormalUser = userRoles.Contains("NormalUser");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to delete this order
            if (isNormalUser && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only delete your own orders.";
                return RedirectToAction(nameof(Index));
            }

            // Only allow deletion of pending orders
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            var (canDelete, message) = await _referenceCheckService.CanDeleteOrderAsync(id);
            if (!canDelete)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Order deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Orders/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user can cancel this order
            if (!await _userRoleService.CanCancelOrder(currentUser, order.Status))
            {
                TempData["ErrorMessage"] = "You don't have permission to cancel this order.";
                return RedirectToAction(nameof(Index));
            }

            // For NormalUser - only allow cancelling their own orders
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isNormalUser = userRoles.Contains("NormalUser");
            if (isNormalUser && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only cancel your own orders.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderState.Cancelled;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Send notification to the order creator
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "Order Cancelled",
                $"Your order (ID: {order.Id}) from {order.StartLocation} to {order.Destination} has been cancelled.",
                RelatedTable.Order,
                order.Id
            );

            TempData["SuccessMessage"] = "Order cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Orders/Approve/5
        [HttpPost, ActionName("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can approve orders
            if (!await _userRoleService.CanApproveRejectOrder(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to approve orders.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Only allow approval of pending orders
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be approved.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderState.Approved;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Send notification to the order creator
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "Order Approved",
                $"Your order (ID: {order.Id}) from {order.StartLocation} to {order.Destination} has been approved. A trip will be created for you soon.",
                RelatedTable.Order,
                order.Id
            );

            // Send notification to FleetManager after approval
            var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
            var notificationTitle = "Order Approved";
            var notificationMessage = $"Order (ID: {order.Id}) has been approved. Please create a trip for this order.";
            foreach (var user in fleetManagers)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    notificationTitle,
                    notificationMessage,
                    RelatedTable.Order,
                    order.Id
                );
            }

            TempData["SuccessMessage"] = "Order approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Orders/Reject/5
        [HttpPost, ActionName("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can reject orders
            if (!await _userRoleService.CanApproveRejectOrder(currentUser))
            {
                TempData["ErrorMessage"] = "You don't have permission to reject orders.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Only allow rejection of pending orders
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be rejected.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderState.Rejected;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Send notification to the order creator
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "Order Rejected",
                $"Your order (ID: {order.Id}) from {order.StartLocation} to {order.Destination} has been rejected. Please contact support for more information.",
                RelatedTable.Order,
                order.Id
            );

            TempData["SuccessMessage"] = "Order rejected successfully.";
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

        private async Task<bool> CheckResourceAvailabilityAsync(Order order)
        {
            // Check if there are available vehicles of the requested type
            var availableVehicles = await _context.Vehicles
                .Where(v => v.Type == order.VehicleType && v.Status == VehicleState.available)
                .AnyAsync();
            if (!availableVehicles) return false;

            // Check if there are available drivers
            var availableDrivers = await _context.Drivers
                .Where(d => d.DriverStatus == DriverState.Available)
                .AnyAsync();
            if (!availableDrivers) return false;

            // Check for schedule conflicts
            var hasScheduleConflict = await _context.Orders
                .Where(o => o.Status == OrderState.Approved && o.Trip != null)
                .Where(o => (order.TripStartDate >= o.TripStartDate && order.TripStartDate <= o.TripEndDate) ||
                            (order.TripEndDate >= o.TripStartDate && order.TripEndDate <= o.TripEndDate))
                .AnyAsync();
            
            return !hasScheduleConflict;
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
