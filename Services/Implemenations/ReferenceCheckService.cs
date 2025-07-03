using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
    public class ReferenceCheckService : IReferenceCheckService
    {
        private readonly SmartFleetContext _context;

        public ReferenceCheckService(SmartFleetContext context)
        {
            _context = context;
        }

        public async Task<(bool canDelete, string message)> CanDeleteVehicleAsync(int vehicleId)
        {
            var hasTrips = await _context.Trips.AnyAsync(t => t.VehicleId == vehicleId);
            if (hasTrips)
                return (false, "Cannot delete vehicle because it has associated trips.");

            var hasMaintenances = await _context.Maintenances.AnyAsync(m => m.VehicleId == vehicleId);
            if (hasMaintenances)
                return (false, "Cannot delete vehicle because it has maintenance records.");

            var hasLocations = await _context.VehicleLocations.AnyAsync(vl => vl.VehicleId == vehicleId);
            if (hasLocations)
                return (false, "Cannot delete vehicle because it has location records.");

            return (true, "Vehicle can be deleted.");
        }

        public async Task<(bool canDelete, string message)> CanDeleteDriverAsync(string driverId)
        {
            var hasTrips = await _context.Trips.AnyAsync(t => t.DriverId == driverId);
            if (hasTrips)
                return (false, "Cannot delete driver because they have associated trips.");

            return (true, "Driver can be deleted.");
        }

        public async Task<(bool canDelete, string message)> CanDeleteOrderAsync(int orderId)
        {
            var hasTrips = await _context.Trips.AnyAsync(t => t.OrderId == orderId);
            if (hasTrips)
                return (false, "Cannot delete order because it has associated trips.");

            return (true, "Order can be deleted.");
        }

        public async Task<(bool canDelete, string message)> CanDeleteTripAsync(int tripId)
        {
            // Trips can be deleted if they are in Cancelled or Completed state
            var trip = await _context.Trips.FindAsync(tripId);
            if (trip == null)
                return (true, "Trip can be deleted.");

            if (trip.Status == TripState.InProgress)
                return (false, "Cannot delete trip because it is currently in progress.");

            if (trip.Status == TripState.Scheduled)
                return (false, "Cannot delete trip because it is scheduled. Cancel it first.");

            return (true, "Trip can be deleted.");
        }

        public async Task<(bool canDelete, string message)> CanDeleteGeofenceAsync(int geofenceId)
        {
            var hasVehicles = await _context.Vehicles.AnyAsync(v => v.GeofenceId == geofenceId);
            if (hasVehicles)
                return (false, "Cannot delete geofence because it has associated vehicles.");

            return (true, "Geofence can be deleted.");
        }

        public async Task<(bool canDelete, string message)> CanDeleteSimCardAsync(int simCardId)
        {
            var hasVehicles = await _context.Vehicles.AnyAsync(v => v.SimCardId == simCardId);
            if (hasVehicles)
                return (false, "Cannot delete SIM card because it is assigned to a vehicle.");

            return (true, "SIM card can be deleted.");
        }

        public async Task<(bool canDelete, string message)> CanDeleteMaintenanceAsync(int maintenanceId)
        {
            var maintenance = await _context.Maintenances.FindAsync(maintenanceId);
            if (maintenance == null)
                return (true, "Maintenance record can be deleted.");

            if (maintenance.RepairStatus == RepairState.in_progress)
                return (false, "Cannot delete maintenance record because repair is in progress.");

            return (true, "Maintenance record can be deleted.");
        }
    }
} 