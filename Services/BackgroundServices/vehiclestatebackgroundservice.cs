using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services.Implemenations;
using SmartFleet.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFleet.Services.BackgroundServices
{
    public class VehicleStateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleStateBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Update every 5 minutes

        public VehicleStateBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<VehicleStateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Vehicle State Background Service started");

            using var timer = new PeriodicTimer(_period);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateVehicleStatesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating vehicle states");
                }
            }

            _logger.LogInformation("Vehicle State Background Service stopped");
        }

        private async Task UpdateVehicleStatesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var vehicleStateService = scope.ServiceProvider.GetRequiredService<IVehicleStateManagementService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<VehicleStateBackgroundService>>();
            var context = scope.ServiceProvider.GetRequiredService<SmartFleetContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var userRoleService = scope.ServiceProvider.GetRequiredService<IUserRoleService>();

            try
            {
                await vehicleStateService.UpdateVehicleStatesAsync();
                logger.LogDebug("Vehicle states updated successfully");

                // --- License Expiry Notification Logic ---
                var now = DateTime.UtcNow;
                var vehicles = await context.Vehicles.ToListAsync();
                var fleetManagers = await userRoleService.GetUsersByRole("FleetManager");
                foreach (var vehicle in vehicles)
                {
                    if (vehicle.RegistrationExpiryDate.HasValue && vehicle.RegistrationExpiryDate.Value.Date < now.Date)
                    {
                        // Avoid duplicate notifications: check if an unread notification for this vehicle/license expiry already exists
                        bool alreadyNotified = await context.Notifications.AnyAsync(n =>
                            n.UserId != null &&
                            fleetManagers.Select(fm => fm.Id).Contains(n.UserId) &&
                            n.Title.Contains("Vehicle License Expired") &&
                            n.RelatedTable == RelatedTable.Vehicle &&
                            n.RelatedId == vehicle.Id &&
                            !n.IsRead
                        );
                        if (!alreadyNotified)
                        {
                            string title = $"Vehicle License Expired";
                            string message = $"The license for vehicle {vehicle.Model} ({vehicle.LicensePlate}) expired on {vehicle.RegistrationExpiryDate:yyyy-MM-dd}. Please renew it.";
                            foreach (var manager in fleetManagers)
                            {
                                await notificationService.CreateNotificationAsync(
                                    manager.Id,
                                    title,
                                    message,
                                    RelatedTable.Vehicle,
                                    vehicle.Id
                                );
                            }
                        }
                    }
                }
                // --- End License Expiry Notification Logic ---
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating vehicle states in background service");
            }
        }
    }
} 