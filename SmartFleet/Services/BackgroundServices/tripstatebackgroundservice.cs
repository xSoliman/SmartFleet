using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartFleet.Services.Implemenations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFleet.Services.BackgroundServices
{
    public class TripStateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TripStateBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1); // Update every minute

        public TripStateBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TripStateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Trip State Background Service started");

            using var timer = new PeriodicTimer(_period);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var tripStateService = scope.ServiceProvider.GetRequiredService<ITripStateManagementService>();
                    
                    await tripStateService.UpdateTripStatesAsync();
                    _logger.LogDebug("Trip states updated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating trip states");
                }
            }

            _logger.LogInformation("Trip State Background Service stopped");
        }
    }
} 