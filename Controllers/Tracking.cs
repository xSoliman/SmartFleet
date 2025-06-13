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
                .Select(v => new
                {
                    v.Id,
                    v.Model,
                    v.Type,
                    v.LicensePlate,
                    v.Status,
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
                        .FirstOrDefault()
                })
                .ToList();

            return Json(vehiclesWithLocations);
        }
    }
}
