using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
   

    public class TripStateManagementService : ITripStateManagementService
    {
        private readonly SmartFleetContext _context;
        private readonly ILogger<TripStateManagementService> _logger;
        private readonly IVehicleStateManagementService _vehicleStateService;
        private readonly IDriverStatusManagementService _driverStatusService;
        private readonly INotificationService _notificationService;
        private readonly HashSet<string> _notifiedDrivers = new(); // To avoid duplicate notifications in one run

        public TripStateManagementService(SmartFleetContext context, ILogger<TripStateManagementService> logger, 
            IVehicleStateManagementService vehicleStateService, IDriverStatusManagementService driverStatusService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _vehicleStateService = vehicleStateService;
            _driverStatusService = driverStatusService;
            _notificationService = notificationService;
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
                        
                        // Update driver state when trip status changes
                        if (trip.DriverId != null)
                        {
                            await _driverStatusService.UpdateSingleDriverStatusAsync(trip.DriverId);
                            _logger.LogInformation($"Updated driver {trip.DriverId} status due to trip {trip.Id} state change");
                        }
                        
                        // Send notification to the order creator about automatic status change
                        if (trip.Order != null)
                        {
                            string statusMessage = "";
                            string title = "";
                            
                            switch (newStatus)
                            {
                                case TripState.InProgress:
                                    title = "Trip Started Automatically";
                                    statusMessage = "has started automatically based on the scheduled time";
                                    break;
                                case TripState.Completed:
                                    title = "Trip Completed Automatically";
                                    statusMessage = "has been completed automatically based on the scheduled end time";
                                    break;
                                default:
                                    title = "Trip Status Updated";
                                    statusMessage = $"status has been updated to {newStatus}";
                                    break;
                            }
                            
                            await _notificationService.CreateNotificationAsync(
                                trip.Order.UserId,
                                title,
                                $"Your trip (ID: {trip.Id}) from {trip.Order.StartLocation} to {trip.Order.Destination} {statusMessage}.",
                                RelatedTable.Trip,
                                trip.Id
                            );
                        }
                        
                        // If trip just completed or cancelled, reset vehicle geofence to default
                        if (newStatus == TripState.Completed || newStatus == TripState.Cancelled)
                        {
                            var vehicle = await _context.Vehicles.FindAsync(trip.VehicleId);
                            if (vehicle != null)
                            {
                                var defaultGeofence = await _context.Geofences.FirstOrDefaultAsync(g => g.IsDefault);
                                vehicle.GeofenceId = defaultGeofence?.Id;
                                _logger.LogInformation($"Vehicle {vehicle.Id} geofence reset to default after trip {trip.Id} completion/cancellation.");
                            }
                        }
                    }

                    // Notify driver 1 day before trip
                    if (trip.Status == TripState.Scheduled && trip.DriverId != null)
                    {
                        var timeToStart = trip.Order.TripStartDate - now;
                        if (timeToStart.TotalMinutes <= 1440 && timeToStart.TotalMinutes > 1410) // 1 day window (30 min tolerance)
                        {
                            var notifyKey = $"{trip.Id}_1d";
                            if (!_notifiedDrivers.Contains(notifyKey))
                            {
                                await _notificationService.CreateNotificationAsync(
                                    trip.DriverId,
                                    "Trip Reminder (1 Day)",
                                    $"Your trip (ID: {trip.Id}) from {trip.Order.StartLocation} to {trip.Order.Destination} starts in 1 day at {trip.Order.TripStartDate:yyyy-MM-dd HH:mm}.",
                                    RelatedTable.Trip,
                                    trip.Id
                                );
                                _notifiedDrivers.Add(notifyKey);
                            }
                        }
                        // Notify 30 minutes before
                        if (timeToStart.TotalMinutes <= 30 && timeToStart.TotalMinutes > 0)
                        {
                            var notifyKey = $"{trip.Id}_30m";
                            if (!_notifiedDrivers.Contains(notifyKey))
                            {
                                await _notificationService.CreateNotificationAsync(
                                    trip.DriverId,
                                    "Trip Reminder (30 Minutes)",
                                    $"Your trip (ID: {trip.Id}) from {trip.Order.StartLocation} to {trip.Order.Destination} starts in 30 minutes at {trip.Order.TripStartDate:yyyy-MM-dd HH:mm}.",
                                    RelatedTable.Trip,
                                    trip.Id
                                );
                                _notifiedDrivers.Add(notifyKey);
                            }
                        }
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
                    
                    // Update driver state when trip status changes
                    if (trip.DriverId != null)
                    {
                        await _driverStatusService.UpdateSingleDriverStatusAsync(trip.DriverId);
                        _logger.LogInformation($"Updated driver {trip.DriverId} status due to trip {tripId} state change");
                    }
                    
                    // Send notification to the order creator about automatic status change
                    if (trip.Order != null)
                    {
                        string statusMessage = "";
                        string title = "";
                        
                        switch (newStatus)
                        {
                            case TripState.InProgress:
                                title = "Trip Started Automatically";
                                statusMessage = "has started automatically based on the scheduled time";
                                break;
                            case TripState.Completed:
                                title = "Trip Completed Automatically";
                                statusMessage = "has been completed automatically based on the scheduled end time";
                                break;
                            default:
                                title = "Trip Status Updated";
                                statusMessage = $"status has been updated to {newStatus}";
                                break;
                        }
                        
                        await _notificationService.CreateNotificationAsync(
                            trip.Order.UserId,
                            title,
                            $"Your trip (ID: {trip.Id}) from {trip.Order.StartLocation} to {trip.Order.Destination} {statusMessage}.",
                            RelatedTable.Trip,
                            trip.Id
                        );
                    }
                    
                    // If trip just completed or cancelled, reset vehicle geofence to default
                    if (newStatus == TripState.Completed || newStatus == TripState.Cancelled)
                    {
                        var vehicle = await _context.Vehicles.FindAsync(trip.VehicleId);
                        if (vehicle != null)
                        {
                            var defaultGeofence = await _context.Geofences.FirstOrDefaultAsync(g => g.IsDefault);
                            vehicle.GeofenceId = defaultGeofence?.Id;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Vehicle {vehicle.Id} geofence reset to default after trip {trip.Id} completion/cancellation.");
                        }
                    }
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