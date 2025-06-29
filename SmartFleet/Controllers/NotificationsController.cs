using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SmartFleet.Hubs;

namespace SmartFleet.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SmartFleetContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationsController(INotificationService notificationService, UserManager<ApplicationUser> userManager, SmartFleetContext context, IHubContext<NotificationHub> hubContext)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        public async Task<IActionResult> GetNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);

            return Json(new { success = true, notifications });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            try
            {
                if (request?.Id == null)
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                await _notificationService.MarkNotificationAsReadAsync(request.Id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class MarkAsReadRequest
        {
            public int Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) 
                    return BadRequest(new { success = false, message = "User not found" });
                
                await _notificationService.MarkAllNotificationAsReadAsync(userId); 
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "User not found" });
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

                if (notification == null)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                // Store the notification info before deletion for SignalR
                var notificationInfo = new
                {
                    id = notification.Id,
                    wasUnread = !notification.IsRead
                };

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                // Send real-time deletion notification via SignalR
                await _hubContext.Clients.User(user.Id).SendAsync("NotificationDeleted", notificationInfo);

                return Ok(new { success = true, message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
