using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace SmartFleet.Services.Implemenations
{
    public class ValidationService : IValidationService
    {
        private readonly SmartFleetContext _context;

        public ValidationService(SmartFleetContext context)
        {
            _context = context;
        }

        public async Task<bool> IsVehicleAvailableForTripAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeTripId = null)
        {
            // Check if vehicle exists and is not in unavailable states
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null || 
                vehicle.Status == VehicleState.on_trip || 
                vehicle.Status == VehicleState.need_maintenance || 
                vehicle.Status == VehicleState.under_maintenance)
            {
                return false;
            }

            // Check for conflicting trips
            var conflictingTrips = await _context.Trips
                .Include(t => t.Order)
                .Where(t => t.VehicleId == vehicleId &&
                           t.Id != excludeTripId &&
                           t.Status == TripState.Scheduled &&
                           ((t.Order.TripStartDate <= startDate && t.Order.TripEndDate > startDate) ||
                            (t.Order.TripStartDate < endDate && t.Order.TripEndDate >= endDate) ||
                            (t.Order.TripStartDate >= startDate && t.Order.TripEndDate <= endDate)))
                .AnyAsync();

            return !conflictingTrips;
        }

        public async Task<bool> IsDriverAvailableForTripAsync(string driverId, DateTime startDate, DateTime endDate, int? excludeTripId = null)
        {
            // Check if driver exists and is available
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null || driver.DriverStatus == DriverState.NotAvailable)
            {
                return false;
            }

            // Check for conflicting trips
            var conflictingTrips = await _context.Trips
                .Include(t => t.Order)
                .Where(t => t.DriverId == driverId &&
                           t.Id != excludeTripId &&
                           t.Status == TripState.Scheduled &&
                           ((t.Order.TripStartDate <= startDate && t.Order.TripEndDate > startDate) ||
                            (t.Order.TripStartDate < endDate && t.Order.TripEndDate >= endDate) ||
                            (t.Order.TripStartDate >= startDate && t.Order.TripEndDate <= endDate)))
                .AnyAsync();

            return !conflictingTrips;
        }

        public async Task<bool> IsLicensePlateUniqueAsync(string licensePlate, int? excludeVehicleId = null)
        {
            return !await _context.Vehicles
                .Where(v => v.LicensePlate.ToUpper() == licensePlate.ToUpper() && v.Id != excludeVehicleId)
                .AnyAsync();
        }

        public async Task<bool> IsSimNumberUniqueAsync(string simNumber, int? excludeSimCardId = null)
        {
            return !await _context.SimCards
                .Where(s => s.SimNumber == simNumber && s.Id != excludeSimCardId)
                .AnyAsync();
        }

        public async Task<bool> IsDriverLicenseUniqueAsync(string licenseNumber, string? excludeDriverId = null)
        {
            return !await _context.Drivers
                .Where(d => d.LicenseNumber.ToUpper() == licenseNumber.ToUpper() && d.Id != excludeDriverId)
                .AnyAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, string? excludeUserId = null)
        {
            return !await _context.Users
                .Where(u => u.Email.ToUpper() == email.ToUpper() && u.Id != excludeUserId)
                .AnyAsync();
        }

        public bool AreTripDatesValid(DateTime startDate, DateTime endDate)
        {
            return startDate < endDate && startDate > DateTime.Now;
        }

        public bool IsVehicleCapacitySufficient(int vehicleCapacity, int passengerCount)
        {
            return vehicleCapacity >= passengerCount && passengerCount > 0;
        }

        public bool AreCoordinatesValid(decimal latitude, decimal longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        public bool IsGeofenceRadiusValid(decimal radiusMeters)
        {
            return radiusMeters > 0 && radiusMeters <= 50000; // Max 50km radius
        }

        public bool IsPhoneNumberValid(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Egyptian phone number format: 01[0-2]XXXXXXXX or 015XXXXXXXX
            var pattern = @"^01[0-2]\d{8}|015\d{8}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

        public bool IsEmailValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public bool IsPasswordValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return false;

            // At least one letter and one number
            var hasLetter = Regex.IsMatch(password, @"[a-zA-Z]");
            var hasNumber = Regex.IsMatch(password, @"\d");

            return hasLetter && hasNumber;
        }
    }
} 