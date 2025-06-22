using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Models;
using SmartFleet.Services;
using System;
using System.Threading.Tasks;
using SmartFleet.Data;
using System.Linq;

namespace SmartFleet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly IDistanceCalculationService _distanceService;

        public LocationController(SmartFleetContext context, IDistanceCalculationService distanceService)
        {
            _context = context;
            _distanceService = distanceService;
        }

        // Simple model to receive GPS data
        public class GpsDataDto
        {
            public int VehicleId { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public decimal Speed { get; set; }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateLocation([FromBody] GpsDataDto gpsData)
        {
            try
            {
                // Create new location entry
                var location = new VehicleLocation
                {
                    VehicleId = gpsData.VehicleId,
                    Latitude = gpsData.Latitude,
                    Longitude = gpsData.Longitude,
                    Speed = gpsData.Speed, // Store the speed value from ESP
                    Timestamp = DateTime.Now // Use server time for timestamp
                };

                // Get the last location for this vehicle
                var lastLocation = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == gpsData.VehicleId)
                    .OrderByDescending(vl => vl.Timestamp)
                    .FirstOrDefaultAsync();

                // Add to database
                _context.VehicleLocations.Add(location);
                await _context.SaveChangesAsync();

                // Update vehicle's total distance traveled
                var vehicle = await _context.Vehicles.FindAsync(gpsData.VehicleId);
                if (vehicle != null && lastLocation != null)
                {
                    var segmentDistance = _distanceService.CalculateDistance(
                        lastLocation.Latitude, lastLocation.Longitude,
                        gpsData.Latitude, gpsData.Longitude
                    );
                    vehicle.TotalDistanceTraveled += segmentDistance;
                    vehicle.UpdatedAt = DateTime.Now;
                    _context.Vehicles.Update(vehicle);
                    await _context.SaveChangesAsync();
                }

                // Check if this vehicle is currently on a trip and update distance
                await UpdateTripDistance(gpsData.VehicleId);

                return Ok(new { success = true, message = "Location updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private async Task UpdateTripDistance(int vehicleId)
        {
            try
            {
                // Find active trip for this vehicle
                var activeTrip = await _context.Trips
                    .Include(t => t.Order)
                    .FirstOrDefaultAsync(t => t.VehicleId == vehicleId && 
                                            t.Status == TripState.InProgress);

                if (activeTrip != null)
                {
                    // Calculate new distance
                    var newDistance = _distanceService.CalculateTripDistance(activeTrip.Id, _context);
                    
                    // Update trip distance
                    activeTrip.Distance = newDistance;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the location update
                // In a production environment, you'd want to use proper logging
                Console.WriteLine($"Error updating trip distance: {ex.Message}");
            }
        }
    }
}