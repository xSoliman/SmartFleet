using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Models;
using SmartFleet.Services;
using System;
using System.Threading.Tasks;
using SmartFleet.Data;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using SmartFleet.Hubs;

namespace SmartFleet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly IDistanceCalculationService _distanceService;
        private readonly IHubContext<TrackingHub> _trackingHub;

        public LocationController(SmartFleetContext context, IDistanceCalculationService distanceService, IHubContext<TrackingHub> trackingHub)
        {
            _context = context;
            _distanceService = distanceService;
            _trackingHub = trackingHub;
        }

        // Simple GPS data model
        public class GpsDataDto
        {
            public string SimCardNumber { get; set; }  // SimCard number instead of VehicleId
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public decimal Speed { get; set; }
            public string? DeviceId { get; set; }      // Optional device identifier
            public string? DeviceModel { get; set; }   // Optional device model
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateLocation([FromBody] GpsDataDto gpsData)
        {
            try
            {
                // Find vehicle by SimCard number
                var vehicle = await _context.Vehicles
                    .Include(v => v.SimCard)
                    .FirstOrDefaultAsync(v => v.SimCard.SimNumber == gpsData.SimCardNumber && 
                                            v.SimCard.Status == SimCardStatus.Active);

                if (vehicle == null)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid SimCard number or SimCard not assigned to any vehicle" 
                    });
                }

                // Create new location entry
                var location = new VehicleLocation
                {
                    VehicleId = vehicle.Id,
                    Latitude = gpsData.Latitude,
                    Longitude = gpsData.Longitude,
                    Speed = gpsData.Speed,
                    Timestamp = DateTime.Now
                };

                // Get the last location for this vehicle
                var lastLocation = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == vehicle.Id)
                    .OrderByDescending(vl => vl.Timestamp)
                    .FirstOrDefaultAsync();

                // Add to database
                _context.VehicleLocations.Add(location);
                await _context.SaveChangesAsync();

                // Update vehicle's total distance traveled
                if (lastLocation != null)
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
                await UpdateTripDistance(vehicle.Id);

                // Send real-time update to all connected tracking clients
                var vehicleUpdateData = new
                {
                    vehicleId = vehicle.Id,
                    vehicleModel = vehicle.Model,
                    vehicleType = vehicle.Type,
                    licensePlate = vehicle.LicensePlate,
                    status = vehicle.Status,
                    totalDistanceTraveled = vehicle.TotalDistanceTraveled,
                    simCardNumber = vehicle.SimCard?.SimNumber,
                    simCardStatus = vehicle.SimCard?.Status,
                    latestLocation = new
                    {
                        latitude = gpsData.Latitude,
                        longitude = gpsData.Longitude,
                        speed = gpsData.Speed,
                        timestamp = DateTime.Now
                    }
                };

                await _trackingHub.Clients.Group("TrackingGroup").SendAsync("ReceiveVehicleLocationUpdate", vehicleUpdateData);

                return Ok(new { 
                    success = true, 
                    message = "Location updated successfully",
                    vehicleId = vehicle.Id,
                    vehiclePlate = vehicle.LicensePlate
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Alternative endpoint for SimCard-based updates (more secure)
        [HttpPost("update-by-simcard")]
        public async Task<IActionResult> UpdateLocationBySimCard([FromBody] GpsDataDto gpsData)
        {
            return await UpdateLocation(gpsData);
        }

        // Endpoint to get vehicle information by SimCard number
        [HttpGet("vehicle-by-simcard/{simCardNumber}")]
        public async Task<IActionResult> GetVehicleBySimCard(string simCardNumber)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.SimCard)
                .Where(v => v.SimCard.SimNumber == simCardNumber)
                .Select(v => new
                {
                    v.Id,
                    v.Model,
                    v.LicensePlate,
                    v.Type,
                    v.Status,
                    SimCardNumber = v.SimCard.SimNumber,
                    SimCardStatus = v.SimCard.Status
                })
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehicle not found for this SimCard" });
            }

            return Ok(vehicle);
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
                Console.WriteLine($"Error updating trip distance: {ex.Message}");
            }
        }
    }
}