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

namespace SmartFleet.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(SmartFleetContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string searchUserId, string searchStartLocation, string searchEndLocation, 
            VehicleType? typeFilter, OrderState? stateFilter, DateTime? startDate, DateTime? endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdminUser = userRoles.Any(r => r == "FleetManager" || r == "SysSupport" || r == "commissioner");

            var orders = _context.Orders.Include(o => o.User).AsQueryable();

            // Role-based filtering - NormalUser, Driver, MaintenanceManager see only their own orders
            if (!isAdminUser)
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
                orders = orders.Where(o => o.TripStartLocation.Contains(searchStartLocation));
            }

            if (!string.IsNullOrEmpty(searchEndLocation))
            {
                orders = orders.Where(o => o.TripEndLocation.Contains(searchEndLocation));
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
            orders = orders.OrderByDescending(o => o.CreatedAt);

            var viewModel = new OrderViewModel
            {
                Orders = await orders.ToListAsync(),
                SearchUserId = searchUserId,
                SearchStartLocation = searchStartLocation,
                SearchEndLocation = searchEndLocation,
                TypeFilter = typeFilter,
                StateFilter = stateFilter,
                StartDate = startDate,
                EndDate = endDate,
                IsAdminUser = isAdminUser
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

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
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
        public async Task<IActionResult> Create([Bind("Id,VehicleType,PassengerCount,TripStartLocation,TripEndLocation,TripStartDate,TripEndDate,Reason,CreatedAt")] Order order)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            order.Status = OrderState.Pending; // Always set status to Pending
            order.UserId = currentUser.Id;

            _context.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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

            // Only allow cancellation of pending orders (for NormalUser, Driver, MaintenanceManager)
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            // Update order status to cancelled
            order.Status = OrderState.Cancelled;
            
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order has been cancelled successfully.";
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

        // GET: Orders/Cancel/5 (for confirmation)
        public async Task<IActionResult> Cancel(int? id)
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

            // Check if user has permission to cancel this order
            if (!isAdminUser && order.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // Only allow cancellation of pending orders
            if (order.Status != OrderState.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
