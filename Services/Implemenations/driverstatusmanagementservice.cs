using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SmartFleet.Hubs;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
   
    public class DriverStatusManagementService : IDriverStatusManagementService
    {
        private readonly SmartFleetContext _context;
        private readonly ILogger<DriverStatusManagementService> _logger;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public DriverStatusManagementService(SmartFleetContext context, ILogger<DriverStatusManagementService> logger, 
            IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _logger = logger;
            _notificationHub = notificationHub;
        }

        /// <summary>
        /// Updates all driver statuses based on their current trip assignments
        /// </summary>
        public async Task UpdateDriverStatusesAsync()
        {
            try
            {
                var drivers = await _context.Drivers.ToListAsync();
                
                foreach (var driver in drivers)
                {
                    await UpdateSingleDriverStatusAsync(driver.Id);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated statuses for {drivers.Count} drivers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver statuses");
            }
        }

        /// <summary>
        /// Updates a single driver's status based on their current trip assignments
        /// </summary>
        public async Task UpdateSingleDriverStatusAsync(string driverId)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Trips)
                    .FirstOrDefaultAsync(d => d.Id == driverId);

                if (driver == null)
                {
                    _logger.LogWarning($"Driver {driverId} not found");
                    return;
                }

                var originalStatus = driver.DriverStatus;
                var newStatus = DetermineDriverStatus(driver);

                if (originalStatus != newStatus)
                {
                    driver.DriverStatus = newStatus;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Driver {driver.UserName} status changed from {originalStatus} to {newStatus}");
                    
                    // Send real-time notification about driver state change
                    var driverUpdateData = new
                    {
                        DriverId = driver.Id,
                        DriverName = driver.UserName,
                        OldStatus = originalStatus.ToString(),
                        NewStatus = newStatus.ToString(),
                        Timestamp = DateTime.Now,
                        Message = $"Driver {driver.UserName} status changed from {originalStatus} to {newStatus}"
                    };
                    
                    // Send to all connected clients
                    await _notificationHub.Clients.All.SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                    
                    // Send specific notification to the driver
                    await _notificationHub.Clients.Group($"User_{driver.Id}").SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                    
                    // Send to fleet managers group
                    await _notificationHub.Clients.Group("FleetManagers").SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver {driverId} status");
            }
        }

        /// <summary>
        /// Updates driver status when assigned to a trip
        /// </summary>
        public async Task UpdateDriverStatusOnTripAssignmentAsync(string driverId)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Trips)
                    .FirstOrDefaultAsync(d => d.Id == driverId);

                if (driver == null)
                {
                    _logger.LogWarning($"Driver {driverId} not found for trip assignment");
                    return;
                }

                var originalStatus = driver.DriverStatus;
                
                // Check if driver has any active trips (Scheduled or InProgress)
                var hasScheduledTrips = driver.Trips?.Any(t => t.Status == TripState.Scheduled) ?? false;
                var hasInProgressTrips = driver.Trips?.Any(t => t.Status == TripState.InProgress) ?? false;

                if (hasInProgressTrips && driver.DriverStatus != DriverState.OnTrip)
                {
                    driver.DriverStatus = DriverState.OnTrip;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Driver {driver.UserName} status set to OnTrip due to in-progress trip");
                }
                else if (hasScheduledTrips && !hasInProgressTrips && driver.DriverStatus != DriverState.AssignedOnScheduledTrip)
                {
                    driver.DriverStatus = DriverState.AssignedOnScheduledTrip;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Driver {driver.UserName} status set to AssignedOnScheduledTrip due to scheduled trip");
                }
                
                // Send real-time notification if status changed
                if (originalStatus != driver.DriverStatus)
                {
                    var driverUpdateData = new
                    {
                        DriverId = driver.Id,
                        DriverName = driver.UserName,
                        OldStatus = originalStatus.ToString(),
                        NewStatus = driver.DriverStatus.ToString(),
                        Timestamp = DateTime.Now,
                        Message = $"Driver {driver.UserName} assigned to trip - Status: {driver.DriverStatus}",
                        TripAssignment = true
                    };
                    
                    // Send to all connected clients
                    await _notificationHub.Clients.All.SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                    
                    // Send specific notification to the driver
                    await _notificationHub.Clients.Group($"User_{driver.Id}").SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                    
                    // Send to fleet managers group
                    await _notificationHub.Clients.Group("FleetManagers").SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver {driverId} status on trip assignment");
            }
        }

        /// <summary>
        /// Updates driver status when a trip is completed or cancelled
        /// </summary>
        public async Task UpdateDriverStatusOnTripCompletionAsync(string driverId)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Trips)
                    .FirstOrDefaultAsync(d => d.Id == driverId);

                if (driver == null)
                {
                    _logger.LogWarning($"Driver {driverId} not found for trip completion");
                    return;
                }

                var originalStatus = driver.DriverStatus;
                
                // Check if driver still has any active trips
                var hasScheduledTrips = driver.Trips?.Any(t => t.Status == TripState.Scheduled) ?? false;
                var hasInProgressTrips = driver.Trips?.Any(t => t.Status == TripState.InProgress) ?? false;

                if (!hasScheduledTrips && !hasInProgressTrips && 
                    (driver.DriverStatus == DriverState.OnTrip || driver.DriverStatus == DriverState.AssignedOnScheduledTrip))
                {
                    // Set to Available if no active trips
                    driver.DriverStatus = DriverState.Available;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Driver {driver.UserName} status set to Available after trip completion");
                }
                else if (hasScheduledTrips && !hasInProgressTrips && driver.DriverStatus == DriverState.OnTrip)
                {
                    // If only scheduled trips remain, set to AssignedOnScheduledTrip
                    driver.DriverStatus = DriverState.AssignedOnScheduledTrip;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Driver {driver.UserName} status set to AssignedOnScheduledTrip after in-progress trip completion");
                }
                
                // Send real-time notification if status changed
                if (originalStatus != driver.DriverStatus)
                {
                    var driverUpdateData = new
                    {
                        DriverId = driver.Id,
                        DriverName = driver.UserName,
                        OldStatus = originalStatus.ToString(),
                        NewStatus = driver.DriverStatus.ToString(),
                        Timestamp = DateTime.Now,
                        Message = $"Driver {driver.UserName} trip completed - Status: {driver.DriverStatus}",
                        TripCompletion = true
                    };
                    
                    // Send to all connected clients
                    await _notificationHub.Clients.All.SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                    
                    // Send specific notification to the driver
                    await _notificationHub.Clients.Group($"User_{driver.Id}").SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                    
                    // Send to fleet managers group
                    await _notificationHub.Clients.Group("FleetManagers").SendAsync("ReceiveDriverStateUpdate", driverUpdateData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver {driverId} status on trip completion");
            }
        }

        /// <summary>
        /// Determines the appropriate driver status based on their trip assignments
        /// </summary>
        private DriverState DetermineDriverStatus(Driver driver)
        {
            // Check if driver has any active trips
            var hasScheduledTrips = driver.Trips?.Any(t => t.Status == TripState.Scheduled) ?? false;
            var hasInProgressTrips = driver.Trips?.Any(t => t.Status == TripState.InProgress) ?? false;

            if (hasInProgressTrips)
            {
                return DriverState.OnTrip;
            }
            else if (hasScheduledTrips)
            {
                return DriverState.AssignedOnScheduledTrip;
            }
            else
            {
                // Default to Available if no active trips
                return DriverState.Available;
            }
        }
    }
} 