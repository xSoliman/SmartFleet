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

namespace SmartFleet.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;

        public OrdersController(SmartFleetContext context, UserManager<ApplicationUser> userManager, 
            INotificationService notificationService, IUserRoleService userRoleService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string searchUserId, string searchStartLocation, string searchDestination, 
            VehicleType? typeFilter, OrderState? stateFilter, DateTime? startDate, DateTime? endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");
            var isCommissioner = userRoles.Contains("commissioner");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");

            var orders = _context.Orders.Include(o => o.User).AsQueryable();

            // Role-based filtering
            if (isFleetManager)
            {
                // FleetManager can only see pending and approved orders
                orders = orders.Where(o => o.Status == OrderState.Pending || o.Status == OrderState.Approved);
                // Include Trips for FleetManager to check if trip exists
                orders = orders.Include(o => o.Trip);
            }
            else if (!isAdminUser)
            {
                // For NormalUser, Driver, MaintenanceManager - show only their own orders
                orders = orders.Where(o => o.UserId == currentUser.Id);
            }

            // Original filters (only for admin users)
            if (isAdminUser && !string.IsNullOrEmpty(searchUserId))
            {
                orders = orders.Where(o => o.User != null && o.User.UserName.Contains(searchUserId));
            }

            if (!string.IsNullOrEmpty(searchStartLocation))
            {
                orders = orders.Where(o => o.StartLocation.Contains(searchStartLocation));
            }

            if (!string.IsNullOrEmpty(searchDestination))
            {
                orders = orders.Where(o => o.Destination.Contains(searchDestination));
            }

            if (isAdminUser && typeFilter.HasValue)
            {
                orders = orders.Where(o => o.VehicleType == typeFilter.Value);
            }

            if (stateFilter.HasValue)
            {
                orders = orders.Where(o => o.Status == stateFilter.Value);
            }

            // Date range filtering
            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt.Date <= endDate.Value.Date);
            }

            // Sort by submission date in descending order (newest first)
            // For FleetManager, also sort by status (Pending first, then Approved)
            if (isFleetManager)
            {
                orders = orders.OrderByDescending(o => o.Status == OrderState.Approved && !_context.Trips.Any(t => t.OrderId == o.Id))
                              .ThenBy(o => o.CreatedAt); // Oldest first for FleetManager
            }
            else
            {
                orders = orders.OrderByDescending(o => o.CreatedAt);
            }

            var viewModel = new OrderViewModel
            {
                Orders = await orders.ToListAsync(),
                SearchUserId = searchUserId,
                SearchStartLocation = searchStartLocation,
                SearchDestination = searchDestination,
                TypeFilter = typeFilter,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate,
                IsAdminUser = isAdminUser,
                IsCommissioner = isCommissioner,
                IsFleetManager = isFleetManager,
                IsSysSupport = isSysSupport,
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
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");
            var isCommissioner = userRoles.Contains("commissioner");
            var isFleetManager = userRoles.Contains("FleetManager");
            var isSysSupport = userRoles.Contains("SysSupport");

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Include Trip for FleetManager to check if trip exists
            if (isFleetManager)
            {
                order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Trip)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }

            // Check if user has permission to view this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
            }

            ViewBag.IsCommissioner = isCommissioner;
            ViewBag.IsFleetManager = isFleetManager;
            ViewBag.IsSysSupport = isSysSupport;
            ViewBag.IsAdminUser = isAdminUser;

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
            order.Status = OrderState.Pending; // Always set status to Pending
            order.UserId = User.FindFirst(ClaimTypes.NameIdentifier).ToString();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
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
                var sysSupportUsers = await _userRoleService.GetUsersByRole("SysSupport");
                
                // Get users with FleetManager role
                var fleetManagerUsers = await _userRoleService.GetUsersByRole("FleetManager");
                
                // Get users with commissioner role
                var commissionerUsers = await _userRoleService.GetUsersByRole("commissioner");

                // Combine all users who should receive notifications
                var allNotificationUsers = new List<ApplicationUser>();
                allNotificationUsers.AddRange(sysSupportUsers);
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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to edit this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // For NormalUser, Driver, MaintenanceManager - only allow editing of pending orders
            if (!isAdminUser && order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be edited.";
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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");

            // Check if user has permission to edit this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // For NormalUser, Driver, MaintenanceManager - only allow editing of pending orders
            if (!isAdminUser && order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be edited.";
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
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to delete this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
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
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to delete this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");
            var isSysSupport = userRoles.Contains("SysSupport");
            var isFleetManager = userRoles.Contains("FleetManager");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to cancel this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // SysSupport can only cancel their own orders, not other users' orders
            if (isSysSupport && order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only cancel your own orders.";
                return RedirectToAction(nameof(Index));
            }

            // Only allow cancellation of pending orders
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderState.Cancelled;
            _context.Update(order);
            await _context.SaveChangesAsync();

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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isCommissioner = userRoles.Contains("commissioner");
            var isSysSupport = userRoles.Contains("SysSupport");

            // Only Commissioner and SysSupport can approve orders
            if (!isCommissioner && !isSysSupport)
            {
                TempData["ErrorMessage"] = "You don't have permission to approve orders.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders.FindAsync(id);
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

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isCommissioner = userRoles.Contains("commissioner");
            var isSysSupport = userRoles.Contains("SysSupport");

            // Only Commissioner and SysSupport can reject orders
            if (!isCommissioner && !isSysSupport)
            {
                TempData["ErrorMessage"] = "You don't have permission to reject orders.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders.FindAsync(id);
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

            TempData["SuccessMessage"] = "Order rejected successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
