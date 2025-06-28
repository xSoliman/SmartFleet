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

        public OrdersController(SmartFleetContext context, UserManager<ApplicationUser> userManager, 
            INotificationService notificationService, IUserRoleService userRoleService, IPaginationService paginationService, ISearchService searchService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
            _paginationService = paginationService;
            _searchService = searchService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string searchUserId, string searchStartLocation, string searchDestination, 
            VehicleType? typeFilter, OrderState? stateFilter, DateTime? startDate, DateTime? endDate, int pageNumber = 1)
        {
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

            // Get orders based on user role
            IQueryable<Order> ordersQuery = _context.Orders.Include(o => o.User);

            // Include Trip for FleetManager and Commissioner to check if trip exists
            if (isFleetManager || isCommissioner)
            {
                ordersQuery = ordersQuery.Include(o => o.Trip);
            }

            // Role-based filtering
            if (isFleetManager)
            {
                // FleetManager can only see pending and approved orders
                ordersQuery = ordersQuery.Where(o => o.Status == OrderState.Pending || o.Status == OrderState.Approved);
            }
            else if (isCommissioner)
            {
                // Commissioner can see all orders (for approval/rejection)
                // Include Trips for Commissioner to see trip status
                ordersQuery = ordersQuery.AsQueryable();
            }
            else if (isDriver)
            {
                // Driver has no access to orders
                TempData["ErrorMessage"] = "Drivers don't have access to orders.";
                return RedirectToAction("Index", "Home");
            }
            else if (isMaintenanceManager)
            {
                // Maintenance Manager has no access to orders
                TempData["ErrorMessage"] = "Maintenance managers don't have access to orders.";
                return RedirectToAction("Index", "Home");
            }
            else if (isNormalUser)
            {
                // NormalUser sees only their own orders
                ordersQuery = ordersQuery.Where(o => o.UserId == currentUser.Id);
            }
            else if (isSysSupport)
            {
                // SysSupport sees all orders
                ordersQuery = ordersQuery.AsQueryable();
            }
            else
            {
                ordersQuery = ordersQuery.AsQueryable();
            }

            // Original filters (only for admin users)
            var filters = new List<System.Linq.Expressions.Expression<Func<Order, bool>>>();
            if ((isFleetManager || isSysSupport || isCommissioner) && !string.IsNullOrEmpty(searchUserId))
                filters.Add(o => o.User != null && o.User.UserName.Contains(searchUserId));
            if (!string.IsNullOrEmpty(searchStartLocation))
                filters.Add(o => o.StartLocation.Contains(searchStartLocation));
            if (!string.IsNullOrEmpty(searchDestination))
                filters.Add(o => o.Destination.Contains(searchDestination));
            if ((isFleetManager || isSysSupport || isCommissioner) && typeFilter.HasValue)
                filters.Add(o => o.VehicleType == typeFilter.Value);
            if (stateFilter.HasValue)
                filters.Add(o => o.Status == stateFilter.Value);
            if (startDate.HasValue)
                filters.Add(o => o.CreatedAt.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                filters.Add(o => o.CreatedAt.Date <= endDate.Value.Date);
            ordersQuery = _searchService.ApplyFilters(ordersQuery, filters);

            // Sort by priority: For FleetManager, show 'Create Trip' (approved, no trip) first, then pending, then others. For Commissioner/others, pending first, then 'Create Trip', then others
            if (isFleetManager)
            {
                ordersQuery = ordersQuery.OrderBy(o => o.Status == OrderState.Approved && o.Trip == null ? 0 :
                                            o.Status == OrderState.Pending ? 1 : 2)
                               .ThenBy(o => o.CreatedAt);
            }
            else
            {
                ordersQuery = ordersQuery.OrderBy(o => o.Status == OrderState.Pending ? 0 :
                                            o.Status == OrderState.Approved && o.Trip == null ? 1 : 2)
                               .ThenBy(o => o.CreatedAt);
            }

            int pageSize = 10;
            int totalCount = await ordersQuery.CountAsync();
            var pagedOrders = await _paginationService.GetPaginatedAsync(ordersQuery, pageNumber, pageSize);

            var viewModel = new OrderViewModel
            {
                Orders = pagedOrders,
                SearchUserId = searchUserId,
                SearchStartLocation = searchStartLocation,
                SearchDestination = searchDestination,
                TypeFilter = typeFilter,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate,
                IsAdminUser = isFleetManager || isSysSupport || isCommissioner,
                IsCommissioner = isCommissioner,
                IsFleetManager = isFleetManager,
                IsSysSupport = isSysSupport,
                CurrentUserId = currentUser.Id,
                CanCreateOrder = await _userRoleService.CanCreateOrder(currentUser)
            };

            // Populate resource availability for commissioner and fleet manager
            if ((isCommissioner || isFleetManager) && viewModel.Orders != null)
            {
                viewModel.ResourceAvailability = new Dictionary<int, string>();
                foreach (var order in viewModel.Orders)
                {
                    viewModel.ResourceAvailability[order.Id] = await GetOrderResourceAvailabilityAsync(order);
                }
            }

            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.SearchUserId = searchUserId;
            ViewBag.SearchStartLocation = searchStartLocation;
            ViewBag.SearchDestination = searchDestination;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.StateFilter = stateFilter;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

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

            var order = await _context.Orders.FindAsync(id);
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

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,VehicleType,PassengerCount,TripStartLocation,TripEndLocation,TripStartDate,TripEndDate,Reason,Status,CreatedAt")] Order order)
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

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
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
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");

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

            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
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

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
