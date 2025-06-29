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
using Newtonsoft.Json;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly IDistanceCalculationService _distanceService;
        private readonly IHubContext<TrackingHub> _trackingHub;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;

        public LocationController(SmartFleetContext context, IDistanceCalculationService distanceService, IHubContext<TrackingHub> trackingHub, INotificationService notificationService, IUserRoleService userRoleService)
        {
            _context = context;
            _distanceService = distanceService;
            _trackingHub = trackingHub;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
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
                    .Include(v => v.Geofence)
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

                // --- Notification Logic ---
                // 1. Speed Exceeded
                decimal speedThreshold = 120; // kph
                if (gpsData.Speed > speedThreshold)
                {
                    var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                    string title = "Speed Limit Exceeded";
                    string message = $"Vehicle {vehicle.Model} ({vehicle.LicensePlate}) exceeded speed limit: {gpsData.Speed} kph.";
                    foreach (var user in fleetManagers)
                    {
                        await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.Vehicle, vehicle.Id);
                    }
                }

                // 2. SimCard/Signal Status
                // SimCard inactive
                if (vehicle.SimCard.Status != SimCardStatus.Active)
                {
                    var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                    string title = "SimCard Inactive";
                    string message = $"SimCard for vehicle {vehicle.Model} ({vehicle.LicensePlate}) is inactive.";
                    foreach (var user in fleetManagers)
                    {
                        await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.SimCard, vehicle.SimCard.Id);
                    }
                }
                // GPS signal loss (no update for > 5 min)
                if (lastLocation != null && (DateTime.Now - lastLocation.Timestamp).TotalMinutes > 5)
                {
                    var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                    string title = "GPS Signal Lost";
                    string message = $"No GPS update for vehicle {vehicle.Model} ({vehicle.LicensePlate}) for over 5 minutes.";
                    foreach (var user in fleetManagers)
                    {
                        await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.VehicleLocatoin, vehicle.Id);
                    }
                }
                // Unauthorized Vehicle Use: Notify if vehicle is moving outside of scheduled trips or working hours
                if (gpsData.Speed > 0)
                {
                    // Check if vehicle is on an active trip (Scheduled or InProgress)
                    var now = DateTime.Now;
                    var hasActiveTrip = await _context.Trips.AnyAsync(t => t.VehicleId == vehicle.Id && (t.Status == TripState.InProgress || t.Status == TripState.Scheduled)
                        && t.Order.TripStartDate <= now && t.Order.TripEndDate >= now);

                    // Define working hours (e.g., 7am to 7pm)
                    var startHour = 7;
                    var endHour = 19;
                    var isWorkingHours = now.Hour >= startHour && now.Hour < endHour;

                    if (!hasActiveTrip && !isWorkingHours)
                    {
                        var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                        string title = "Unauthorized Vehicle Use";
                        string message = $"Vehicle {vehicle.Model} ({vehicle.LicensePlate}) is moving outside of scheduled trips or working hours.";
                        foreach (var user in fleetManagers)
                        {
                            await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.Vehicle, vehicle.Id);
                        }
                    }
                }
                // Geofence Breach Detection
                if (vehicle.GeofenceId.HasValue && vehicle.Geofence != null)
                {
                    bool isInside = false;
                    if (vehicle.Geofence.Type == GeofenceType.Circle)
                    {
                        // Calculate distance between vehicle and geofence center (Haversine formula)
                        double R = 6371000; // Earth radius in meters
                        double lat1 = (double)vehicle.Geofence.CenterLat * Math.PI / 180.0;
                        double lon1 = (double)vehicle.Geofence.CenterLng * Math.PI / 180.0;
                        double lat2 = (double)gpsData.Latitude * Math.PI / 180.0;
                        double lon2 = (double)gpsData.Longitude * Math.PI / 180.0;
                        double dLat = lat2 - lat1;
                        double dLon = lon2 - lon1;
                        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                                   Math.Cos(lat1) * Math.Cos(lat2) *
                                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                        double distance = R * c;
                        isInside = distance <= (double)vehicle.Geofence.RadiusMeters;
                    }
                    else if (vehicle.Geofence.Type == GeofenceType.Polygon && !string.IsNullOrEmpty(vehicle.Geofence.PolygonJson))
                    {
                        // Parse polygon points from JSON
                        var polygon = JsonConvert.DeserializeObject<List<List<double>>>(vehicle.Geofence.PolygonJson);
                        if (polygon != null && polygon.Count > 2)
                        {
                            double lat = (double)gpsData.Latitude;
                            double lng = (double)gpsData.Longitude;
                            isInside = IsPointInPolygon(lat, lng, polygon);
                        }
                    }

                    // Only notify on transition from inside to outside
                    if ((vehicle.WasInsideGeofence == null || vehicle.WasInsideGeofence == true) && !isInside)
                    {
                        var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                        string title = "Geofence Breach";
                        string message = $"Geofence breach detected for vehicle {vehicle.Model} ({vehicle.LicensePlate}). The vehicle has left the designated area.";
                        foreach (var user in fleetManagers)
                        {
                            await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.Vehicle, vehicle.Id);
                        }
                    }
                    // Update last state
                    vehicle.WasInsideGeofence = isInside;
                    _context.Vehicles.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                // --- End Notification Logic ---

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

        // Point-in-polygon algorithm (Ray Casting)
        private bool IsPointInPolygon(double lat, double lng, List<List<double>> polygon)
        {
            int n = polygon.Count;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = polygon[i][0], yi = polygon[i][1];
                double xj = polygon[j][0], yj = polygon[j][1];
                bool intersect = ((yi > lng) != (yj > lng)) &&
                    (lat < (xj - xi) * (lng - yi) / (yj - yi + 1e-12) + xi);
                if (intersect)
                    inside = !inside;
            }
            return inside;
        }
    }
}