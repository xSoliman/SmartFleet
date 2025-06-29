using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services.Implemenations;
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

            try
            {
                await vehicleStateService.UpdateVehicleStatesAsync();
                logger.LogDebug("Vehicle states updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating vehicle states in background service");
            }
        }
    }
} 