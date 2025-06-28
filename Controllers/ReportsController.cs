using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.ViewModel;

namespace SmartFleet.Controllers
{
    [Authorize(Roles = "SysSupport,FleetManager")]
    public class ReportsController : Controller
    {
        private readonly SmartFleetContext _context;

        public ReportsController(SmartFleetContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reportViewModel = new ReportsViewModel();

            // Get counts for different entities
            reportViewModel.TotalVehicles = await _context.Vehicles.CountAsync();
            reportViewModel.ActiveVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleState.available);
            reportViewModel.VehiclesOnTrip = await _context.Vehicles.CountAsync(v => v.Status == VehicleState.on_trip);
            reportViewModel.VehiclesNeedMaintenance = await _context.Vehicles.CountAsync(v => v.Status == VehicleState.need_maintenance);

            reportViewModel.TotalDrivers = await _context.Drivers.CountAsync();
            reportViewModel.AvailableDrivers = await _context.Drivers.CountAsync(d => d.DriverStatus == DriverState.Available);
            reportViewModel.DriversOnTrip = await _context.Drivers.CountAsync(d => d.DriverStatus == DriverState.OnTrip);
            reportViewModel.BusyDrivers = await _context.Drivers.CountAsync(d => d.DriverStatus == DriverState.AssignedOnScheduledTrip);

            reportViewModel.TotalTrips = await _context.Trips.CountAsync();
            reportViewModel.CompletedTrips = await _context.Trips.CountAsync(t => t.Status == TripState.Completed);
            reportViewModel.ActiveTrips = await _context.Trips.CountAsync(t => t.Status == TripState.InProgress);
            reportViewModel.ScheduledTrips = await _context.Trips.CountAsync(t => t.Status == TripState.Scheduled);

            reportViewModel.TotalOrders = await _context.Orders.CountAsync();
            reportViewModel.PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderState.Pending);
            reportViewModel.ApprovedOrders = await _context.Orders.CountAsync(o => o.Status == OrderState.Approved);
            reportViewModel.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == OrderState.Approved && _context.Trips.Any(t => t.OrderId == o.Id && t.Status == TripState.Completed));

            reportViewModel.TotalMaintenances = await _context.Maintenances.CountAsync();
            reportViewModel.PendingMaintenances = await _context.Maintenances.CountAsync(m => m.RepairStatus == RepairState.pending);
            reportViewModel.CompletedMaintenances = await _context.Maintenances.CountAsync(m => m.RepairStatus == RepairState.completed);
            reportViewModel.InProgressMaintenances = await _context.Maintenances.CountAsync(m => m.RepairStatus == RepairState.in_progress);

            reportViewModel.TotalUsers = await _context.Users.CountAsync();
            reportViewModel.ActiveUsers = await _context.Users.CountAsync(u => u.AccountStatus);
            reportViewModel.TotalNotifications = await _context.Notifications.CountAsync();
            reportViewModel.UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);

            // Get data for charts
            await GetChartData(reportViewModel);

            // Get recent activities
            await GetRecentActivities(reportViewModel);

            // Calculate fleet utilization
            reportViewModel.FleetUtilizationPercentage = reportViewModel.TotalVehicles > 0 
                ? Math.Round((double)reportViewModel.VehiclesOnTrip / reportViewModel.TotalVehicles * 100, 1) 
                : 0;

            // Calculate driver efficiency
            reportViewModel.DriverEfficiencyPercentage = reportViewModel.TotalDrivers > 0 
                ? Math.Round((double)(reportViewModel.DriversOnTrip + reportViewModel.BusyDrivers) / reportViewModel.TotalDrivers * 100, 1) 
                : 0;

            // Calculate total distance traveled
            reportViewModel.TotalDistanceTraveled = await _context.Vehicles.SumAsync(v => v.TotalDistanceTraveled);

            return View(reportViewModel);
        }

        private async Task GetChartData(ReportsViewModel model)
        {
            // Trips per month (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var tripsPerMonth = await _context.Trips
                .Where(t => t.CreatedAt >= sixMonthsAgo)
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Count = g.Count() 
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            model.TripsPerMonthLabels = tripsPerMonth.Select(x => $"{x.Year}-{x.Month:00}").ToList();
            model.TripsPerMonthData = tripsPerMonth.Select(x => x.Count).ToList();

            // Vehicle status distribution
            var vehicleStatusData = await _context.Vehicles
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            model.VehicleStatusLabels = vehicleStatusData.Select(x => x.Status).ToList();
            model.VehicleStatusData = vehicleStatusData.Select(x => x.Count).ToList();

            // Driver status distribution
            var driverStatusData = await _context.Drivers
                .GroupBy(d => d.DriverStatus)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            model.DriverStatusLabels = driverStatusData.Select(x => x.Status).ToList();
            model.DriverStatusData = driverStatusData.Select(x => x.Count).ToList();

            // Orders per month (last 6 months)
            var ordersPerMonth = await _context.Orders
                .Where(o => o.CreatedAt >= sixMonthsAgo)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Count = g.Count() 
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            model.OrdersPerMonthLabels = ordersPerMonth.Select(x => $"{x.Year}-{x.Month:00}").ToList();
            model.OrdersPerMonthData = ordersPerMonth.Select(x => x.Count).ToList();

            // Maintenance trends (last 6 months)
            var maintenancePerMonth = await _context.Maintenances
                .Where(m => m.CreatedAt >= sixMonthsAgo)
                .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
                .Select(g => new { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Count = g.Count() 
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            model.MaintenancePerMonthLabels = maintenancePerMonth.Select(x => $"{x.Year}-{x.Month:00}").ToList();
            model.MaintenancePerMonthData = maintenancePerMonth.Select(x => x.Count).ToList();
        }

        private async Task GetRecentActivities(ReportsViewModel model)
        {
            // Recent trips
            model.RecentTrips = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new RecentTripDto
                {
                    Id = t.Id,
                    VehicleLicensePlate = t.Vehicle.LicensePlate,
                    DriverName = t.Driver.UserName ?? "",
                    StartLocation = t.Order.StartLocation,
                    Destination = t.Order.Destination,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt,
                    Distance = t.Distance
                })
                .ToListAsync();

            // Recent orders
            model.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    UserName = o.User.UserName ?? "",
                    StartLocation = o.StartLocation,
                    Destination = o.Destination,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    VehicleType = o.VehicleType.ToString(),
                    PassengerCount = o.PassengerCount
                })
                .ToListAsync();

            // Recent maintenance
            model.RecentMaintenances = await _context.Maintenances
                .Include(m => m.Vehicle)
                .Include(m => m.ReportedUser)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .Select(m => new RecentMaintenanceDto
                {
                    Id = m.Id,
                    VehicleLicensePlate = m.Vehicle != null ? m.Vehicle.LicensePlate : "N/A",
                    IssueDescription = m.IssueDescription,
                    Priority = m.Priority.ToString(),
                    Status = m.RepairStatus.ToString(),
                    CreatedAt = m.CreatedAt,
                    ReportedBy = m.ReportedUser != null ? (m.ReportedUser.UserName ?? "N/A") : "N/A"
                })
                .ToListAsync();

            // Vehicle performance data
            var vehiclesWithTrips = await _context.Vehicles
                .Select(v => new
                {
                    Vehicle = v,
                    TotalTrips = _context.Trips.Where(t => t.VehicleId == v.Id).Count()
                })
                .OrderByDescending(x => x.Vehicle.TotalDistanceTraveled)
                .Take(10)
                .ToListAsync();

            model.VehiclePerformance = vehiclesWithTrips.Select(x => new VehiclePerformanceDto
            {
                LicensePlate = x.Vehicle.LicensePlate,
                Model = x.Vehicle.Model,
                TotalDistance = x.Vehicle.TotalDistanceTraveled,
                TotalTrips = x.TotalTrips,
                Status = x.Vehicle.Status.ToString()
            }).ToList();

            // Driver performance data - using a completely different approach to avoid inheritance issues
            var allDrivers = await _context.Drivers.ToListAsync();
            var driverPerformanceList = new List<DriverPerformanceDto>();

            foreach (var driver in allDrivers.Take(10))
            {
                var totalDistance = await _context.Trips.Where(t => t.DriverId == driver.Id).SumAsync(t => (decimal?)t.Distance) ?? 0;
                var totalTrips = await _context.Trips.Where(t => t.DriverId == driver.Id).CountAsync();

                driverPerformanceList.Add(new DriverPerformanceDto
                {
                    Name = driver.UserName ?? "",
                    LicenseNumber = driver.LicenseNumber,
                    TotalDistance = totalDistance,
                    TotalTrips = totalTrips,
                    Status = driver.DriverStatus.ToString()
                });
            }

            model.DriverPerformance = driverPerformanceList.OrderByDescending(x => x.TotalDistance).Take(10).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport([FromBody] ReportRequestModel request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ReportType))
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var report = request.ReportType.ToLower() switch
                {
                    "trips" => await GenerateTripsReport(request.StartDate, request.EndDate),
                    "vehicles" => await GenerateVehiclesReport(request.StartDate, request.EndDate),
                    "drivers" => await GenerateDriversReport(request.StartDate, request.EndDate),
                    "maintenance" => await GenerateMaintenanceReport(request.StartDate, request.EndDate),
                    _ => throw new ArgumentException("Invalid report type")
                };

                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ReportRequestModel
        {
            public string ReportType { get; set; } = "";
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string __RequestVerificationToken { get; set; } = "";
        }

        private async Task<object> GenerateTripsReport(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Trips.Include(t => t.Vehicle).Include(t => t.Driver).Include(t => t.Order).AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);

            var trips = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    Id = t.Id,
                    Vehicle = t.Vehicle.LicensePlate,
                    Driver = t.Driver.UserName,
                    StartLocation = t.Order.StartLocation,
                    Destination = t.Order.Destination,
                    Distance = t.Distance,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return new
            {
                totalTrips = trips.Count,
                totalDistance = trips.Sum(t => t.Distance),
                averageDistance = trips.Any() ? trips.Average(t => t.Distance) : 0,
                trips = trips
            };
        }

        private async Task<object> GenerateVehiclesReport(DateTime? startDate, DateTime? endDate)
        {
            var vehicles = await _context.Vehicles
                .Select(v => new
                {
                    Id = v.Id,
                    LicensePlate = v.LicensePlate,
                    Model = v.Model,
                    Type = v.Type.ToString(),
                    Status = v.Status.ToString(),
                    TotalDistance = v.TotalDistanceTraveled,
                    TotalTrips = _context.Trips.Where(t => t.VehicleId == v.Id).Count(),
                    MaintenanceCount = _context.Maintenances.Where(m => m.VehicleId == v.Id).Count(),
                    CreatedAt = v.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return new
            {
                totalVehicles = vehicles.Count,
                totalDistance = vehicles.Sum(v => v.TotalDistance),
                averageTripsPerVehicle = vehicles.Any() ? vehicles.Average(v => v.TotalTrips) : 0,
                vehicles = vehicles
            };
        }

        private async Task<object> GenerateDriversReport(DateTime? startDate, DateTime? endDate)
        {
            var allDrivers = await _context.Drivers.ToListAsync();
            var driverReports = new List<object>();

            foreach (var driver in allDrivers)
            {
                var totalTrips = await _context.Trips.Where(t => t.DriverId == driver.Id).CountAsync();
                var totalDistance = await _context.Trips.Where(t => t.DriverId == driver.Id).SumAsync(t => (decimal?)t.Distance) ?? 0;

                driverReports.Add(new
                {
                    Id = driver.Id,
                    Name = driver.UserName,
                    Email = driver.Email,
                    LicenseNumber = driver.LicenseNumber,
                    LicenseExpiry = driver.LicenseExpiryDate.ToString("yyyy-MM-dd"),
                    Status = driver.DriverStatus.ToString(),
                    TotalTrips = totalTrips,
                    TotalDistance = totalDistance,
                    AccountStatus = driver.AccountStatus ? "Active" : "Inactive",
                    CreatedAt = driver.CreatedAt.ToString("yyyy-MM-dd")
                });
            }

            var totalTripsSum = 0;
            var totalDistanceSum = 0m;
            var activeDrivers = 0;

            foreach (dynamic driver in driverReports)
            {
                if (driver.AccountStatus == "Active") activeDrivers++;
                totalTripsSum += (int)driver.TotalTrips;
                totalDistanceSum += (decimal)driver.TotalDistance;
            }

            return new
            {
                totalDrivers = driverReports.Count,
                activeDrivers = activeDrivers,
                totalTrips = totalTripsSum,
                totalDistance = totalDistanceSum,
                drivers = driverReports
            };
        }

        private async Task<object> GenerateMaintenanceReport(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Maintenances.Include(m => m.Vehicle).Include(m => m.ReportedUser).AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.CreatedAt <= endDate.Value);

            var maintenances = await query
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new
                {
                    Id = m.Id,
                    Vehicle = m.Vehicle != null ? m.Vehicle.LicensePlate : "N/A",
                    IssueDescription = m.IssueDescription,
                    Priority = m.Priority.ToString(),
                    Status = m.RepairStatus.ToString(),
                    ReportedBy = m.ReportedUser != null ? m.ReportedUser.UserName : "N/A",
                    CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    UpdatedAt = m.UpdatedAt.HasValue ? m.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm") : ""
                })
                .ToListAsync();

            return new
            {
                totalMaintenances = maintenances.Count,
                pendingMaintenances = maintenances.Count(m => m.Status == "pending"),
                completedMaintenances = maintenances.Count(m => m.Status == "completed"),
                highPriorityMaintenances = maintenances.Count(m => m.Priority == "high"),
                maintenances = maintenances
            };
        }
    }
} 