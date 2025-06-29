using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Hubs;
using SmartFleet.Models;

namespace SmartFleet.Services
{
   
    public class NotificationService : INotificationService
    {
        private readonly SmartFleetContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(SmartFleetContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, RelatedTable relatedTable, int? relatedId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    RelatedTable = relatedTable,
                    RelatedId = relatedId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Determine notification type based on title
                string notificationType = DetermineNotificationType(title);

                // Send real-time notification via SignalR to specific user
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
                {
                    id = notification.Id.ToString(),
                    title = notification.Title,
                    message = notification.Message,
                    type = notificationType,
                    createdAt = notification.CreatedAt,
                    isRead = notification.IsRead,
                    userId = notification.UserId
                });

                Console.WriteLine($"📨 Notification sent via SignalR to User_{userId}: {title} (Type: {notificationType})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create notification: {ex.Message}");
                throw new Exception($"Failed to create notification: {ex.Message}", ex);
            }
        }

        private string DetermineNotificationType(string title)
        {
            if (string.IsNullOrEmpty(title))
                return "info";

            title = title.ToLower();

            // Geofence breach - red
            if (title.Contains("geofence breach") || title.Contains("unauthorized vehicle use"))
                return "danger";

            // Completed or accepted - green
            if (title.Contains("completed") || title.Contains("approved") || title.Contains("started"))
                return "success";

            // Rejected - red/orange
            if (title.Contains("rejected") || title.Contains("cancelled") || title.Contains("failed"))
                return "warning";

            // Default - informative
            return "info";
        }

        public async Task CreateBroadcastNotificationAsync(string title, string message, RelatedTable relatedTable, int? relatedId = null)
        {
            try
            {
                // Determine notification type based on title
                string notificationType = DetermineNotificationType(title);

                // Send real-time notification via SignalR to all connected users
                await _hubContext.Clients.Group("AllUsers").SendAsync("ReceiveNotification", new
                {
                    id = Guid.NewGuid().ToString(),
                    title = title,
                    message = message,
                    type = notificationType,
                    createdAt = DateTime.UtcNow,
                    isRead = false,
                    userId = "broadcast"
                });

                Console.WriteLine($"📢 Broadcast notification sent via SignalR: {title} (Type: {notificationType})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send broadcast notification: {ex.Message}");
                throw new Exception($"Failed to send broadcast notification: {ex.Message}", ex);
            }
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId);
                    
                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                    
                    // Send real-time update to the client
                    await _hubContext.Clients.User(notification.UserId).SendAsync("NotificationMarkedAsRead", new
                    {
                        id = notification.Id,
                        isRead = notification.IsRead
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to mark notification as read: {ex.Message}", ex);
            }
        }

        public async Task MarkAllNotificationAsReadAsync(string userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();
                
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }
                
                await _context.SaveChangesAsync();
                
                // Send real-time update to the client
                await _hubContext.Clients.User(userId).SendAsync("AllNotificationsMarkedAsRead");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to mark all notifications as read: {ex.Message}", ex);
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get user notifications: {ex.Message}", ex);
            }
        }
    }
}