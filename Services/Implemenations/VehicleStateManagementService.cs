using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFleet.Services.Implemenations
{
  

    public class VehicleStateManagementService : IVehicleStateManagementService
    {
        private readonly SmartFleetContext _context;
        private readonly ILogger<VehicleStateManagementService> _logger;

        public VehicleStateManagementService(SmartFleetContext context, ILogger<VehicleStateManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Updates all vehicle states based on their current trip assignments
        /// </summary>
        public async Task UpdateVehicleStatesAsync()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Include(v => v.Trips)
                    .ToListAsync();

                var updatedCount = 0;

                foreach (var vehicle in vehicles)
                {
                    var originalStatus = vehicle.Status;
                    var newStatus = DetermineVehicleState(vehicle);

                    if (originalStatus != newStatus)
                    {
                        vehicle.Status = newStatus;
                        vehicle.UpdatedAt = DateTime.Now;
                        updatedCount++;
                        _logger.LogInformation($"Vehicle {vehicle.LicensePlate} status changed from {originalStatus} to {newStatus}");
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated statuses for {updatedCount} vehicles");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle states");
            }
        }

        /// <summary>
        /// Updates a single vehicle's state based on its current trip assignments
        /// </summary>
        public async Task UpdateSingleVehicleStateAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Trips)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    _logger.LogWarning($"Vehicle {vehicleId} not found");
                    return;
                }

                var originalStatus = vehicle.Status;
                var newStatus = DetermineVehicleState(vehicle);

                if (originalStatus != newStatus)
                {
                    vehicle.Status = newStatus;
                    vehicle.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Vehicle {vehicle.LicensePlate} status changed from {originalStatus} to {newStatus}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle {vehicleId} state");
            }
        }

        /// <summary>
        /// Updates vehicle state when assigned to a trip
        /// </summary>
        public async Task UpdateVehicleStateOnTripAssignmentAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Trips)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    _logger.LogWarning($"Vehicle {vehicleId} not found for trip assignment");
                    return;
                }

                // Check if vehicle has any active trips (Scheduled or InProgress)
                var hasScheduledTrips = vehicle.Trips?.Any(t => t.Status == TripState.Scheduled) ?? false;
                var hasInProgressTrips = vehicle.Trips?.Any(t => t.Status == TripState.InProgress) ?? false;

                if (hasInProgressTrips && vehicle.Status != VehicleState.on_trip)
                {
                    vehicle.Status = VehicleState.on_trip;
                    vehicle.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Vehicle {vehicle.LicensePlate} status set to on_trip due to in-progress trip");
                }
                else if (hasScheduledTrips && !hasInProgressTrips && vehicle.Status != VehicleState.on_scheduled_trip)
                {
                    vehicle.Status = VehicleState.on_scheduled_trip;
                    vehicle.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Vehicle {vehicle.LicensePlate} status set to on_scheduled_trip due to scheduled trip");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle {vehicleId} status on trip assignment");
            }
        }

        /// <summary>
        /// Updates vehicle state when a trip is completed or cancelled
        /// </summary>
        public async Task UpdateVehicleStateOnTripCompletionAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Trips)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    _logger.LogWarning($"Vehicle {vehicleId} not found for trip completion");
                    return;
                }

                // Check if vehicle still has any active trips
                var hasScheduledTrips = vehicle.Trips?.Any(t => t.Status == TripState.Scheduled) ?? false;
                var hasInProgressTrips = vehicle.Trips?.Any(t => t.Status == TripState.InProgress) ?? false;

                if (!hasScheduledTrips && !hasInProgressTrips && 
                    (vehicle.Status == VehicleState.on_trip || vehicle.Status == VehicleState.on_scheduled_trip))
                {
                    // Set to available if no active trips and not in maintenance
                    if (vehicle.Status != VehicleState.need_maintenance && 
                        vehicle.Status != VehicleState.under_maintenance)
                    {
                        vehicle.Status = VehicleState.available;
                        vehicle.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Vehicle {vehicle.LicensePlate} status set to available after trip completion");
                    }
                }
                else if (hasScheduledTrips && !hasInProgressTrips && vehicle.Status == VehicleState.on_trip)
                {
                    // If only scheduled trips remain, set to on_scheduled_trip
                    vehicle.Status = VehicleState.on_scheduled_trip;
                    vehicle.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Vehicle {vehicle.LicensePlate} status set to on_scheduled_trip after in-progress trip completion");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle {vehicleId} status on trip completion");
            }
        }

        /// <summary>
        /// Determines the appropriate vehicle state based on its trip assignments
        /// </summary>
        private VehicleState DetermineVehicleState(Vehicle vehicle)
        {
            // Don't change state if vehicle is in maintenance
            if (vehicle.Status == VehicleState.need_maintenance || 
                vehicle.Status == VehicleState.under_maintenance)
            {
                return vehicle.Status;
            }

            // Check if vehicle has any active trips
            var hasScheduledTrips = vehicle.Trips?.Any(t => t.Status == TripState.Scheduled) ?? false;
            var hasInProgressTrips = vehicle.Trips?.Any(t => t.Status == TripState.InProgress) ?? false;

            if (hasInProgressTrips)
            {
                return VehicleState.on_trip;
            }
            else if (hasScheduledTrips)
            {
                return VehicleState.on_scheduled_trip;
            }
            else
            {
                // Default to available if no active trips
                return VehicleState.available;
            }
        }
    }
} 