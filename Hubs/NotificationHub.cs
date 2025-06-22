using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SmartFleet.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // This class can be used to send notifications to connected clients.
        // You can add methods here to handle specific notification logic.
        // For example, you might want to send a message to all connected clients:

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal group for targeted notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                Console.WriteLine($"User {userId} connected to notification hub");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove user from their personal group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                Console.WriteLine($"User {userId} disconnected from notification hub");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Method to send notification to a specific user
        public async Task SendNotificationToUser(string userId, object notification)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
        }

        // Method to send notification to all connected clients (for admin notifications)
        public async Task SendNotificationToAll(object notification)
        {
            await Clients.All.SendAsync("ReceiveNotification", notification);
        }

        // Method to send notification to a specific group (for role-based notifications)
        public async Task SendNotificationToGroup(string groupName, object notification)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
        }

        // You can also add methods for specific groups or users if needed.
    }

    [Authorize]
    public class TrackingHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to tracking group for real-time updates
                await Groups.AddToGroupAsync(Context.ConnectionId, "TrackingGroup");
                Console.WriteLine($"User {userId} connected to tracking hub");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove user from tracking group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "TrackingGroup");
                Console.WriteLine($"User {userId} disconnected from tracking hub");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Method to send vehicle location update to all tracking clients
        public async Task SendVehicleLocationUpdate(object vehicleData)
        {
            await Clients.Group("TrackingGroup").SendAsync("ReceiveVehicleLocationUpdate", vehicleData);
        }

        // Method to send vehicle status update to all tracking clients
        public async Task SendVehicleStatusUpdate(object vehicleData)
        {
            await Clients.Group("TrackingGroup").SendAsync("ReceiveVehicleStatusUpdate", vehicleData);
        }
    }
}
