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
    public class OrdersController : Controller
    {
        private readonly SmartFleetContext _context;

        public OrdersController(SmartFleetContext context)
        {
            _context = context;
        }

        // GET: Orders

        // search & filter
        public async Task<IActionResult> Index(string searchUserId, string searchStartLocation, string searchEndLocation, string typeFilter, OrderState? stateFilter)
        {
            var orders = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(searchUserId))
            {
                orders = orders.Where(o => o.User.UserName.Contains(searchUserId));
            }


            if (!string.IsNullOrEmpty(searchStartLocation))
            {
                orders = orders.Where(o => o.TripStartLocation.Contains(searchStartLocation));
            }

            if (!string.IsNullOrEmpty(searchEndLocation))
            {
                orders = orders.Where(o => o.TripEndLocation.Contains(searchEndLocation));
            }

            if (!string.IsNullOrEmpty(typeFilter) && Enum.TryParse<VehicleType>(typeFilter, out var parsedType))
            {
                orders = orders.Where(o => o.VehicleType == parsedType);
            }

            if (stateFilter.HasValue)
            {
                orders = orders.Where(o => o.Status == stateFilter.Value);
            }

            return View(await orders.ToListAsync());
        }


        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewBag.VehicleTypes = new List<SelectListItem>
    {
        new SelectListItem { Value = "Car", Text = "Car" },
        new SelectListItem { Value = "Truck", Text = "Truck" },
        new SelectListItem { Value = "Bus", Text = "Bus" },
        new SelectListItem { Value = "Van", Text = "Van" },
        new SelectListItem { Value = "Motorcycle", Text = "Motorcycle" },
        new SelectListItem { Value = "Other", Text = "Other" }
    };

            ViewBag.UserId = Request.Cookies["UserId"];

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Orders/Create
       
        public async Task<IActionResult> Create([Bind("Id,VehicleType,PassengerCount,TripStartLocation,TripEndLocation,TripStartDate,TripEndDate,Reason,CreatedAt")] Order order)
        {
            order.Status = OrderState.pending; // Always set status to Pending
            order.UserId = Request.Cookies["UserId"];

            _context.Add(order);
            await _context.SaveChangesAsync();

            // إنشاء Cookie تحتوي على OrderId بعد الحفظ بنجاح
            CookieOptions option = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddHours(1), // صلاحية لمدة ساعة
                HttpOnly = true, // تحسين الأمان
                Secure = true // استخدام HTTPS فقط
            };

            Response.Cookies.Append("OrderId", order.Id.ToString(), option);

            return RedirectToAction(nameof(Index));
        }




        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
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

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
