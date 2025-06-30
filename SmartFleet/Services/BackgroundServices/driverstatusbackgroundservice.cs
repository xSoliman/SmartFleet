using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.BackgroundServices
{
    public class DriverStatusBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DriverStatusBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1); // Update every 1 minute for more dynamic updates

        public DriverStatusBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DriverStatusBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Driver Status Background Service started");

            using var timer = new PeriodicTimer(_period);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateDriverStatusesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating driver statuses");
                }
            }

            _logger.LogInformation("Driver Status Background Service stopped");
        }

        private async Task UpdateDriverStatusesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SmartFleetContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DriverStatusBackgroundService>>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var userRoleService = scope.ServiceProvider.GetRequiredService<IUserRoleService>();

            try
            {
                // Get all drivers with their trips
                var drivers = await context.Drivers
                    .Include(d => d.Trips)
                    .ToListAsync();

                var updatedCount = 0;

                foreach (var driver in drivers)
                {
                    var originalStatus = driver.DriverStatus;
                    var newStatus = DetermineDriverStatus(driver);

                    if (originalStatus != newStatus)
                    {
                        driver.DriverStatus = newStatus;
                        updatedCount++;
                        logger.LogInformation($"Driver {driver.UserName} status changed from {originalStatus} to {newStatus}");
                    }
                }

                if (updatedCount > 0)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Updated statuses for {updatedCount} drivers");
                }

                // --- License Expiry Notification Logic ---
                var now = DateTime.UtcNow;
                var fleetManagers = await userRoleService.GetUsersByRole("FleetManager");
                foreach (var driver in drivers)
                {
                    if (driver.LicenseExpiryDate.Date < now.Date)
                    {
                        // Avoid duplicate notifications: check if an unread notification for this driver/license expiry already exists
                        bool alreadyNotified = await context.Notifications.AnyAsync(n =>
                            n.UserId != null &&
                            fleetManagers.Select(fm => fm.Id).Contains(n.UserId) &&
                            n.Title.Contains("Driver License Expired") &&
                            n.RelatedTable == RelatedTable.Driver &&
                            n.RelatedId == null &&
                            !n.IsRead
                        );
                        if (!alreadyNotified)
                        {
                            string title = $"Driver License Expired";
                            string message = $"The license for driver {driver.UserName} (License: {driver.LicenseNumber}) expired on {driver.LicenseExpiryDate:yyyy-MM-dd}. Please renew it.";
                            foreach (var manager in fleetManagers)
                            {
                                await notificationService.CreateNotificationAsync(
                                    manager.Id,
                                    title,
                                    message,
                                    RelatedTable.Driver,
                                    null
                                );
                            }
                        }
                    }
                }
                // --- End License Expiry Notification Logic ---
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating driver statuses in background service");
            }
        }

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