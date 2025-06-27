using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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