using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFleet.Services
{
    public interface ITripStateManagementService
    {
        Task UpdateTripStatesAsync();
        Task UpdateSingleTripStateAsync(int tripId);
    }

    public class TripStateManagementService : ITripStateManagementService
    {
        private readonly SmartFleetContext _context;
        private readonly ILogger<TripStateManagementService> _logger;
        private readonly IVehicleStateManagementService _vehicleStateService;

        public TripStateManagementService(SmartFleetContext context, ILogger<TripStateManagementService> logger, 
            IVehicleStateManagementService vehicleStateService)
        {
            _context = context;
            _logger = logger;
            _vehicleStateService = vehicleStateService;
        }

        /// <summary>
        /// Updates all trip states based on current time and trip schedule
        /// </summary>
        public async Task UpdateTripStatesAsync()
        {
            try
            {
                var now = DateTime.Now;
                var tripsToUpdate = await _context.Trips
                    .Include(t => t.Order)
                    .Where(t => t.Status != TripState.Cancelled) // Don't update cancelled trips
                    .ToListAsync();

                foreach (var trip in tripsToUpdate)
                {
                    var originalStatus = trip.Status;
                    var newStatus = DetermineTripState(trip, now);

                    if (originalStatus != newStatus)
                    {
                        trip.Status = newStatus;
                        _logger.LogInformation($"Trip {trip.Id} status changed from {originalStatus} to {newStatus}");
                        
                        // Update vehicle state when trip status changes
                        await _vehicleStateService.UpdateVehicleStateOnTripAssignmentAsync(trip.VehicleId);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated {tripsToUpdate.Count} trip states");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trip states");
            }
        }

        /// <summary>
        /// Updates a single trip's state
        /// </summary>
        public async Task UpdateSingleTripStateAsync(int tripId)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Order)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip == null)
                {
                    _logger.LogWarning($"Trip {tripId} not found");
                    return;
                }

                if (trip.Status == TripState.Cancelled)
                {
                    return; // Don't update cancelled trips
                }

                var now = DateTime.Now;
                var originalStatus = trip.Status;
                var newStatus = DetermineTripState(trip, now);

                if (originalStatus != newStatus)
                {
                    trip.Status = newStatus;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Trip {tripId} status changed from {originalStatus} to {newStatus}");
                    
                    // Update vehicle state when trip status changes
                    await _vehicleStateService.UpdateVehicleStateOnTripAssignmentAsync(trip.VehicleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating trip {tripId} state");
            }
        }

        /// <summary>
        /// Determines the appropriate trip state based on current time and trip schedule
        /// </summary>
        private TripState DetermineTripState(Trip trip, DateTime currentTime)
        {
            var tripStartTime = trip.Order.TripStartDate;
            var tripEndTime = trip.Order.TripEndDate;

            // If current time is before trip start time
            if (currentTime < tripStartTime)
            {
                return TripState.Scheduled;
            }
            // If current time is between trip start and end time
            else if (currentTime >= tripStartTime && currentTime <= tripEndTime)
            {
                return TripState.InProgress;
            }
            // If current time is after trip end time
            else if (currentTime > tripEndTime)
            {
                return TripState.Completed;
            }
            // Default fallback
            else
            {
                return TripState.Scheduled;
            }
        }
    }
} 