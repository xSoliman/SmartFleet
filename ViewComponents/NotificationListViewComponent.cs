using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Models;
using SmartFleet.Services;

namespace SmartFleet.ViewComponents
{
    public class NotificationListViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationListViewComponent(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return View(new NotificationListViewModel
                {
                    Notifications = new List<Notification>(),
                    UnreadCount = 0
                });
            }

            // Force fresh data by using a direct query instead of cached data
            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
            var unreadCount = notifications.Count(n => !n.IsRead);

            var viewModel = new NotificationListViewModel
            {
                Notifications = notifications,
                UnreadCount = unreadCount
            };

            return View(viewModel);
        }
    }

    public class NotificationListViewModel
    {
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public int UnreadCount { get; set; }
    }
}