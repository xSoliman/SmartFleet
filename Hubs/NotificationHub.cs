using Microsoft.AspNetCore.SignalR;

namespace SmartFleet.Hubs
{
    public class NotificationHub : Hub
    {
        // This class can be used to send notifications to connected clients.
        // You can add methods here to handle specific notification logic.
        // For example, you might want to send a message to all connected clients:

        public async Task SendNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

        // You can also add methods for specific groups or users if needed.
    }
    
}
