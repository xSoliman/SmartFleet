using SmartFleet.Models;

namespace SmartFleet.Services.Interfaces
{
    public interface IValidationService
    {
        /// <summary>
        /// Validates that a vehicle is available for a trip during the specified time period
        /// </summary>
        Task<bool> IsVehicleAvailableForTripAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeTripId = null);
        
        /// <summary>
        /// Validates that a driver is available for a trip during the specified time period
        /// </summary>
        Task<bool> IsDriverAvailableForTripAsync(string driverId, DateTime startDate, DateTime endDate, int? excludeTripId = null);
        
        /// <summary>
        /// Validates that a license plate is unique across all vehicles
        /// </summary>
        Task<bool> IsLicensePlateUniqueAsync(string licensePlate, int? excludeVehicleId = null);
        
        /// <summary>
        /// Validates that a SIM number is unique across all SIM cards
        /// </summary>
        Task<bool> IsSimNumberUniqueAsync(string simNumber, int? excludeSimCardId = null);
        
        /// <summary>
        /// Validates that a driver license number is unique across all drivers
        /// </summary>
        Task<bool> IsDriverLicenseUniqueAsync(string licenseNumber, string? excludeDriverId = null);
        
        /// <summary>
        /// Validates that an email is unique across all users
        /// </summary>
        Task<bool> IsEmailUniqueAsync(string email, string? excludeUserId = null);
        
        /// <summary>
        /// Validates that trip dates are logical (end date after start date)
        /// </summary>
        bool AreTripDatesValid(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Validates that a vehicle capacity can accommodate the passenger count
        /// </summary>
        bool IsVehicleCapacitySufficient(int vehicleCapacity, int passengerCount);
        
        /// <summary>
        /// Validates that coordinates are within valid ranges
        /// </summary>
        bool AreCoordinatesValid(decimal latitude, decimal longitude);
        
        /// <summary>
        /// Validates that a geofence radius is reasonable
        /// </summary>
        bool IsGeofenceRadiusValid(decimal radiusMeters);
        
        /// <summary>
        /// Validates that a phone number format is correct
        /// </summary>
        bool IsPhoneNumberValid(string phoneNumber);
        
        /// <summary>
        /// Validates that an email format is correct
        /// </summary>
        bool IsEmailValid(string email);
        
        /// <summary>
        /// Validates that a password meets security requirements
        /// </summary>
        bool IsPasswordValid(string password);
    }
} 