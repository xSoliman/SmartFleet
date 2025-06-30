using SmartFleet.Models;

namespace SmartFleet.Services.Interfaces
{
    public interface IReferenceCheckService
    {
        Task<(bool canDelete, string message)> CanDeleteVehicleAsync(int vehicleId);
        Task<(bool canDelete, string message)> CanDeleteDriverAsync(string driverId);
        Task<(bool canDelete, string message)> CanDeleteOrderAsync(int orderId);
        Task<(bool canDelete, string message)> CanDeleteTripAsync(int tripId);
        Task<(bool canDelete, string message)> CanDeleteGeofenceAsync(int geofenceId);
        Task<(bool canDelete, string message)> CanDeleteSimCardAsync(int simCardId);
        Task<(bool canDelete, string message)> CanDeleteMaintenanceAsync(int maintenanceId);
    }
} 