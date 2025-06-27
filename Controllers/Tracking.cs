using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System.Linq;

namespace SmartFleet.Controllers
{
    public class Tracking : Controller
    {
        private readonly SmartFleetContext _context;

        public Tracking(SmartFleetContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/tracking/vehicles")]
        public IActionResult GetVehiclesWithLocations()
        {
            var vehiclesWithLocations = _context.Vehicles
                .Include(v => v.VehicleLocations)
                .Include(v => v.SimCard)
                .Select(v => new
                {
                    v.Id,
                    v.Model,
                    v.Type,
                    v.LicensePlate,
                    v.Status,
                    v.TotalDistanceTraveled,
                    v.UpdatedAt,
                    SimCardNumber = v.SimCard != null ? v.SimCard.SimNumber : null,
                    SimCardStatus = v.SimCard != null ? (int?)v.SimCard.Status : null,
                    LatestLocation = v.VehicleLocations
                        .OrderByDescending(vl => vl.Timestamp)
                        .Select(vl => new
                        {
                            vl.Id,
                            vl.Latitude,
                            vl.Longitude,
                            vl.Speed,
                            vl.Timestamp
                        })
                        .FirstOrDefault(),
                    // Get recent locations for path tracking (last 10 points)
                    RecentLocations = v.VehicleLocations
                        .OrderByDescending(vl => vl.Timestamp)
                        .Take(10)
                        .Select(vl => new
                        {
                            vl.Latitude,
                            vl.Longitude,
                            vl.Speed,
                            vl.Timestamp
                        })
                        .OrderBy(vl => vl.Timestamp)
                        .ToList()
                })
                .ToList();

            return Json(vehiclesWithLocations);
        }

        [HttpGet("api/tracking/vehicle/{id}/details")]
        public async Task<IActionResult> GetVehicleDetails(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.VehicleLocations)
                    .Include(v => v.SimCard)
                    .Include(v => v.Trips)
                    .ThenInclude(t => t.Driver)
                    .Include(v => v.Trips)
                    .ThenInclude(t => t.Order)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle == null)
                {
                    return NotFound();
                }

                // Get the latest location
                var latestLocation = vehicle.VehicleLocations
                    .OrderByDescending(vl => vl.Timestamp)
                    .FirstOrDefault();

                // Get active trip
                var activeTrip = vehicle.Trips
                    .Where(t => t.Status == TripState.InProgress)
                    .FirstOrDefault();

                var result = new
                {
                    vehicle.Id,
                    vehicle.Model,
                    vehicle.Type,
                    vehicle.LicensePlate,
                    vehicle.Status,
                    vehicle.TotalDistanceTraveled,
                    vehicle.UpdatedAt,
                    SimCardNumber = vehicle.SimCard?.SimNumber,
                    LatestLocation = latestLocation != null ? new
                    {
                        latestLocation.Latitude,
                        latestLocation.Longitude,
                        latestLocation.Speed,
                        latestLocation.Timestamp
                    } : null,
                    ActiveTrip = activeTrip != null ? new
                    {
                        activeTrip.Id,
                        DriverName = activeTrip.Driver?.UserName,
                        DriverPhone = activeTrip.Driver?.PhoneNumber,
                        OrderDestination = activeTrip.Order?.Destination,
                        activeTrip.Distance,
                        activeTrip.Status
                    } : null
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.StackTrace });
            }
        }

        [HttpGet("api/tracking/vehicle/{id}/path")]
        public async Task<IActionResult> GetVehiclePath(int id, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.VehicleLocations
                .Where(vl => vl.VehicleId == id);

            if (startDate.HasValue)
            {
                query = query.Where(vl => vl.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(vl => vl.Timestamp <= endDate.Value);
            }

            var locations = await query
                .OrderBy(vl => vl.Timestamp)
                .Select(vl => new
                {
                    vl.Latitude,
                    vl.Longitude,
                    vl.Speed,
                    vl.Timestamp
                })
                .ToListAsync();

            return Json(locations);
        }
    }
}
