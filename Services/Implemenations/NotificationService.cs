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

                // Send real-time notification via SignalR
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    createdAt = notification.CreatedAt,
                    isRead = notification.IsRead,
                    relatedTable = notification.RelatedTable,
                    relatedId = notification.RelatedId
                });
            }
            catch (Exception ex)
            {
                // Log the error (you might want to add proper logging here)
                throw new Exception($"Failed to create notification: {ex.Message}", ex);
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