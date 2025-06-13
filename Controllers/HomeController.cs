using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Data;
using SmartFleet.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartFleet.Controllers
{
    public class HomeController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(SmartFleetContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var notifications = _context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                var unreadNotifications = notifications.Count(n => !n.IsRead);

                ViewBag.Notifications = notifications;
                ViewBag.UnreadNotifications = unreadNotifications;
            }
            else
            {
                ViewBag.Notifications = new List<Notification>();
                ViewBag.UnreadNotifications = 0;
            }

            ViewBag.UserCount = _context.Users.Count();
            ViewBag.FleetCount = _context.Vehicles.Count();
            ViewBag.TripsCount = _context.Trips.Count();
            ViewBag.MaintenanceCount = _context.Maintenances.Count();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
