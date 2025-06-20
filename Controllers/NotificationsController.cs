using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;
using System.Threading.Tasks;
using System.Security.Claims;

namespace SmartFleet.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
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
        public async Task<IActionResult> MarkAsRead(int id)
        {
             await _notificationService.MarkNotificationAsReadAsync(id);
            
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return BadRequest("User not found");
            await _notificationService.MarkAllNotificationAsReadAsync( userId); 
            return Ok(new { success = true });
        }
    }
}
