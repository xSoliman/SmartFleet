using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace SmartFleet.Hubs
{
    public class NotificationHub : Hub
    {
        // This class can be used to send notifications to connected clients.
        // You can add methods here to handle specific notification logic.
        // For example, you might want to send a message to all connected clients:

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"🔔 SignalR Connection Attempt - Connection ID: {Context.ConnectionId}");
            Console.WriteLine($"🔔 User Identity: {Context.User?.Identity?.Name ?? "NULL"}");
            Console.WriteLine($"🔔 User Authenticated: {Context.User?.Identity?.IsAuthenticated ?? false}");
            
            // Try to get token from query string if user is not authenticated
            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
                Console.WriteLine($"🔔 Token from query: {(!string.IsNullOrEmpty(token) ? "FOUND" : "NULL")}");
                
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        // Manual JWT validation (simplified for SignalR)
                        var handler = new JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadJwtToken(token);
                        
                        var jwtUserId = jsonToken.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                        var jwtUserName = jsonToken.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
                        
                        Console.WriteLine($"🔔 JWT User ID: {jwtUserId ?? "NULL"}");
                        Console.WriteLine($"🔔 JWT User Name: {jwtUserName ?? "NULL"}");
                        
                        if (!string.IsNullOrEmpty(jwtUserId))
                        {
                            // Add user to their personal group for targeted notifications
                            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{jwtUserId}");
                            
                            // Add to all users group for broadcast notifications
                            await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
                            
                            Console.WriteLine($"✅ JWT User {jwtUserName} (ID: {jwtUserId}) connected to notification hub with connection {Context.ConnectionId}");
                            await base.OnConnectedAsync();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ JWT Token validation failed: {ex.Message}");
                    }
                }
            }
            
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            
            Console.WriteLine($"🔔 User ID: {userId ?? "NULL"}");
            Console.WriteLine($"🔔 User Name: {userName ?? "NULL"}");
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal group for targeted notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                
                // Add to all users group for broadcast notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
                
                Console.WriteLine($"✅ User {userName} (ID: {userId}) connected to notification hub with connection {Context.ConnectionId}");
            }
            else
            {
                Console.WriteLine($"❌ Anonymous user attempted to connect to notification hub with connection {Context.ConnectionId}");
                // Don't reject connection, just don't add to groups
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

        // Method to send driver state update to all connected clients
        public async Task SendDriverStateUpdate(object driverData)
        {
            await Clients.All.SendAsync("ReceiveDriverStateUpdate", driverData);
        }

        // Method to send driver state update to a specific user (driver dashboard)
        public async Task SendDriverStateUpdateToUser(string userId, object driverData)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveDriverStateUpdate", driverData);
        }

        // Method to send driver state update to fleet managers
        public async Task SendDriverStateUpdateToFleetManagers(object driverData)
        {
            await Clients.Group("FleetManagers").SendAsync("ReceiveDriverStateUpdate", driverData);
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
