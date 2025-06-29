using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using System.Diagnostics;

namespace SmartFleet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SafetyController : Controller
    {
        private readonly SmartFleetContext _context;

        public SafetyController(SmartFleetContext context)
        {
            _context = context;
        }

        // صفحة البداية
        [HttpGet]
        [Route("/Safety")]
        public IActionResult Index()
        {
            return View();
        }

        // زر تشغيل المراقبة
        [HttpPost]
        [Route("/Safety/StartMonitoring")]
        public async Task<IActionResult> StartMonitoring()
        {
            try
            {
                var userId = Request.Cookies["UserId"];
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var driver = await _context.Users
                    .OfType<Driver>()
                    .FirstOrDefaultAsync(d => d.Id == userId);

                if (driver == null)
                {
                    TempData["Message"] = "❌ Driver not found.";
                    return RedirectToAction("Index");
                }

                var driverName = driver.UserName;

                var psi = new ProcessStartInfo
                {
                    FileName = @"C:\Users\Sayed\AppData\Local\Programs\Python\Python313\python.exe",
                    Arguments = $"\"C:\\DriverSleepDetector\\Scripts\\eye_blink_detector.py\" \"{driverName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                Process.Start(psi);
                TempData["Message"] = $"✅ Monitoring started for driver: {driverName}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"❌ Failed to start monitoring: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // API لتحديث عدد النعاس
        [HttpPost]
        [Route("/api/drivers/update-drowsiness")]
        public async Task<IActionResult> UpdateDrowsiness([FromBody] DriverDrowsinessDto dto)
        {
            if (string.IsNullOrEmpty(dto.DriverName))
                return BadRequest("Driver name is required.");

            var driver = await _context.Users
                .OfType<Driver>()
                .FirstOrDefaultAsync(d => d.UserName.ToLower() == dto.DriverName.ToLower());

            if (driver == null)
                return NotFound("Driver not found.");

            driver.DrowsinessCount++;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Drowsiness count updated.", count = driver.DrowsinessCount });
        }

        public class DriverDrowsinessDto
        {
            public string DriverName { get; set; }
        }

        // Dashboard السائق
        [HttpGet]
        [Route("/Safety/DriverDashboard")]
        public async Task<IActionResult> DriverDashboard()
        {
            string? driverId = Request.Cookies["UserId"];
            if (string.IsNullOrEmpty(driverId))
                return RedirectToAction("Login", "Account");

            var driver = await _context.Users
                .OfType<Driver>()
                .FirstOrDefaultAsync(d => d.Id == driverId);

            if (driver == null)
                return NotFound();

            return View(driver); // view: Views/Safety/DriverDashboard.cshtml
        }
    }
}
