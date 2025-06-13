using Microsoft.AspNetCore.Mvc;
using SmartFleet.Models;
using System;
using System.Threading.Tasks;
using SmartFleet.Data;

namespace SmartFleet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly SmartFleetContext _context;

        public LocationController(SmartFleetContext context)
        {
            _context = context;
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

                // Add to database
                _context.VehicleLocations.Add(location);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Location updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}