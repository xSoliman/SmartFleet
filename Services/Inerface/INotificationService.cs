using Microsoft.AspNetCore.SignalR;
using SmartFleet.Data;
using SmartFleet.Hubs;
using SmartFleet.Models;

namespace SmartFleet.Services;
public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string message, RelatedTable relatedTable, int? relatedId = null);
    Task CreateBroadcastNotificationAsync(string title, string message, RelatedTable relatedTable, int? relatedId = null);
    Task MarkNotificationAsReadAsync(int notificationId);
    Task<List<Notification>> GetUserNotificationsAsync(string userId);
    Task MarkAllNotificationAsReadAsync( string userId);

}
