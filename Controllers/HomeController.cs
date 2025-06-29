using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Data;
using SmartFleet.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

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
          
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);

                if (userRoles.Contains("NormalUser"))
                {
                    // Normal User Statistics
                    ViewBag.Stat1 = _context.Orders.Count(o => o.UserId == currentUser.Id);
                    ViewBag.Stat2 = _context.Trips.Count(t => t.CreatedBy == currentUser.Id && t.Status == TripState.Completed);
                    ViewBag.Stat3 = _context.Trips.Count(t => t.CreatedBy == currentUser.Id && t.Status == TripState.Scheduled);
                    ViewBag.Stat4 = _context.Orders.Count(o => o.UserId == currentUser.Id && o.Status == OrderState.Pending);
                    
                    ViewBag.Icon1 = "fas fa-file-alt";
                    ViewBag.Icon2 = "fas fa-check-circle";
                    ViewBag.Icon3 = "fas fa-calendar";
                    ViewBag.Icon4 = "fas fa-clock";
                    
                    ViewBag.Label1 = "My Orders";
                    ViewBag.Label2 = "Completed Trips";
                    ViewBag.Label3 = "Scheduled Trips";
                    ViewBag.Label4 = "Pending Orders";
                }
                else if (userRoles.Contains("FleetManager"))
                {
                    // Fleet Manager Statistics
                    ViewBag.Stat1 = _context.Vehicles.Count();
                    ViewBag.Stat2 = _context.Drivers.Count();
                    ViewBag.Stat3 = _context.Trips.Count(t => t.Status == TripState.Completed);
                    ViewBag.Stat4 = _context.Orders.Count();
                    
                    ViewBag.Icon1 = "fas fa-car";
                    ViewBag.Icon2 = "fas fa-users";
                    ViewBag.Icon3 = "fas fa-route";
                    ViewBag.Icon4 = "fas fa-file-alt";
                    
                    ViewBag.Label1 = "Total Vehicles";
                    ViewBag.Label2 = "Total Drivers";
                    ViewBag.Label3 = "Completed Trips";
                    ViewBag.Label4 = "Total Orders";
                }
                else if (userRoles.Contains("commissioner"))
                {
                    // Commissioner Statistics
                    ViewBag.Stat1 = _context.Orders.Count(o => o.Status == OrderState.Approved);
                    ViewBag.Stat2 = _context.Orders.Count(o => o.Status == OrderState.Rejected);
                    ViewBag.Stat3 = _context.Orders.Count(o => o.Status == OrderState.Pending);
                    ViewBag.Stat4 = _context.Orders.Count(o => o.Status == OrderState.Cancelled);
                    
                    ViewBag.Icon1 = "fas fa-check";
                    ViewBag.Icon2 = "fas fa-times";
                    ViewBag.Icon3 = "fas fa-clock";
                    ViewBag.Icon4 = "fas fa-ban";
                    
                    ViewBag.Label1 = "Approved Orders";
                    ViewBag.Label2 = "Rejected Orders";
                    ViewBag.Label3 = "Pending Orders";
                    ViewBag.Label4 = "Cancelled Orders";
                }
                else if (userRoles.Contains("Driver"))
                {
                    // Driver Statistics
                    ViewBag.Stat1 = _context.Trips.Count(t => t.DriverId == currentUser.Id && t.Status == TripState.Completed);
                    ViewBag.Stat2 = _context.Trips.Count(t => t.DriverId == currentUser.Id && t.Status == TripState.Scheduled);
                    ViewBag.Stat3 = _context.Trips.Count(t => t.DriverId == currentUser.Id && t.Status == TripState.InProgress);
                    ViewBag.Stat4 = _context.Trips.Count(t => t.DriverId == currentUser.Id);
                    
                    ViewBag.Icon1 = "fas fa-check-circle";
                    ViewBag.Icon2 = "fas fa-calendar";
                    ViewBag.Icon3 = "fas fa-play-circle";
                    ViewBag.Icon4 = "fas fa-route";
                    
                    ViewBag.Label1 = "Completed Trips";
                    ViewBag.Label2 = "Scheduled Trips";
                    ViewBag.Label3 = "Active Trips";
                    ViewBag.Label4 = "Total Trips";
                }
                else if (userRoles.Contains("MaintenanceManager"))
                {
                    // Maintenance Manager Statistics
                    ViewBag.Stat1 = _context.Maintenances.Count(m => m.RepairStatus == RepairState.pending);
                    ViewBag.Stat2 = _context.Maintenances.Count(m => m.RepairStatus == RepairState.in_progress);
                    ViewBag.Stat3 = _context.Maintenances.Count(m => m.RepairStatus == RepairState.completed);
                    ViewBag.Stat4 = _context.Vehicles.Count(v => v.Status == VehicleState.need_maintenance || v.Status == VehicleState.under_maintenance);
                    
                    ViewBag.Icon1 = "fas fa-clock";
                    ViewBag.Icon2 = "fas fa-tools";
                    ViewBag.Icon3 = "fas fa-check";
                    ViewBag.Icon4 = "fas fa-exclamation-triangle";
                    
                    ViewBag.Label1 = "Pending Repairs";
                    ViewBag.Label2 = "In Progress";
                    ViewBag.Label3 = "Completed";
                    ViewBag.Label4 = "Vehicles Needing Maintenance";
                }
                else if (userRoles.Contains("SysSupport"))
                {
                    // System Support Statistics
                    ViewBag.Stat1 = _context.Users.Count();
                    ViewBag.Stat2 = _context.Notifications.Count();
                    ViewBag.Stat3 = _context.Events.Count();
                    ViewBag.Stat4 = _context.Users.Count(u => !u.AccountStatus);
                    
                    ViewBag.Icon1 = "fas fa-users";
                    ViewBag.Icon2 = "fas fa-bell";
                    ViewBag.Icon3 = "fas fa-calendar-alt";
                    ViewBag.Icon4 = "fas fa-user-times";
                    
                    ViewBag.Label1 = "Total Users";
                    ViewBag.Label2 = "Notifications";
                    ViewBag.Label3 = "System Events";
                    ViewBag.Label4 = "Inactive Users";
                }
                else
                {
                    // Default statistics for unassigned roles
                    ViewBag.Stat1 = _context.Users.Count();
                    ViewBag.Stat2 = _context.Vehicles.Count();
                    ViewBag.Stat3 = _context.Trips.Count();
                    ViewBag.Stat4 = _context.Orders.Count();
                    
                    ViewBag.Icon1 = "fas fa-users";
                    ViewBag.Icon2 = "fas fa-car";
                    ViewBag.Icon3 = "fas fa-route";
                    ViewBag.Icon4 = "fas fa-file-alt";
                    
                    ViewBag.Label1 = "Total Users";
                    ViewBag.Label2 = "Total Vehicles";
                    ViewBag.Label3 = "Total Trips";
                    ViewBag.Label4 = "Total Orders";
                }
            }
            else
            {
                // Default statistics for non-authenticated users
                ViewBag.Stat1 = _context.Users.Count();
                ViewBag.Stat2 = _context.Vehicles.Count();
                ViewBag.Stat3 = _context.Trips.Count();
                ViewBag.Stat4 = _context.Orders.Count();
                
                ViewBag.Icon1 = "fas fa-users";
                ViewBag.Icon2 = "fas fa-car";
                ViewBag.Icon3 = "fas fa-route";
                ViewBag.Icon4 = "fas fa-file-alt";
                
                ViewBag.Label1 = "Total Users";
                ViewBag.Label2 = "Total Vehicles";
                ViewBag.Label3 = "Total Trips";
                ViewBag.Label4 = "Total Orders";
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            ViewBag.UserCount = _context.Users.Count();
            ViewBag.FleetCount = _context.Vehicles.Count();
            ViewBag.TripsCount = _context.Trips.Count();
            ViewBag.MaintenanceCount = _context.Maintenances.Count();
            ViewBag.OrdersCount = _context.Orders.Count();
            ViewBag.DriversCount = _context.Drivers.Count();
            ViewBag.SimCardsCount = _context.SimCards.Count();
            ViewBag.NotificationsCount = _context.Notifications.Count();
            ViewBag.EventsCount = _context.Events.Count();
            ViewBag.GeofencesCount = _context.Geofences.Count();
            ViewBag.LatestEvents = _context.Events.OrderByDescending(e => e.CreatedAt).Take(5).ToList();
            ViewBag.LatestNotifications = _context.Notifications.OrderByDescending(n => n.CreatedAt).Take(5).ToList();
            // Trips per month for last 12 months
            var now = DateTime.Now;
            var trips = _context.Trips.ToList();
            var tripsPerMonth = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .Reverse()
                .Select(d => new {
                    Label = d.ToString("yyyy-MM"),
                    Count = trips.Count(t => t.CreatedAt.Year == d.Year && t.CreatedAt.Month == d.Month)
                }).ToList();
            ViewBag.TripsPerMonthLabels = tripsPerMonth.Select(x => x.Label).ToList();
            ViewBag.TripsPerMonthData = tripsPerMonth.Select(x => x.Count).ToList();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
