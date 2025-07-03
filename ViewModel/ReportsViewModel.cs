namespace SmartFleet.ViewModel
{
    public class ReportsViewModel
    {
        // Vehicle Statistics
        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int VehiclesOnTrip { get; set; }
        public int VehiclesNeedMaintenance { get; set; }
        public decimal TotalDistanceTraveled { get; set; }
        public double FleetUtilizationPercentage { get; set; }

        // Driver Statistics
        public int TotalDrivers { get; set; }
        public int AvailableDrivers { get; set; }
        public int DriversOnTrip { get; set; }
        public int BusyDrivers { get; set; }
        public double DriverEfficiencyPercentage { get; set; }

        // Trip Statistics
        public int TotalTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int ActiveTrips { get; set; }
        public int ScheduledTrips { get; set; }

        // Order Statistics
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ApprovedOrders { get; set; }
        public int CompletedOrders { get; set; }

        // Maintenance Statistics
        public int TotalMaintenances { get; set; }
        public int PendingMaintenances { get; set; }
        public int CompletedMaintenances { get; set; }
        public int InProgressMaintenances { get; set; }

        // User Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }

        // Chart Data for Trips
        public List<string> TripsPerMonthLabels { get; set; } = new List<string>();
        public List<int> TripsPerMonthData { get; set; } = new List<int>();

        // Chart Data for Vehicle Status
        public List<string> VehicleStatusLabels { get; set; } = new List<string>();
        public List<int> VehicleStatusData { get; set; } = new List<int>();

        // Chart Data for Driver Status
        public List<string> DriverStatusLabels { get; set; } = new List<string>();
        public List<int> DriverStatusData { get; set; } = new List<int>();

        // Chart Data for Orders
        public List<string> OrdersPerMonthLabels { get; set; } = new List<string>();
        public List<int> OrdersPerMonthData { get; set; } = new List<int>();

        // Chart Data for Maintenance
        public List<string> MaintenancePerMonthLabels { get; set; } = new List<string>();
        public List<int> MaintenancePerMonthData { get; set; } = new List<int>();

        // Recent Activities
        public List<RecentTripDto> RecentTrips { get; set; } = new List<RecentTripDto>();
        public List<RecentOrderDto> RecentOrders { get; set; } = new List<RecentOrderDto>();
        public List<RecentMaintenanceDto> RecentMaintenances { get; set; } = new List<RecentMaintenanceDto>();

        // Performance Data
        public List<VehiclePerformanceDto> VehiclePerformance { get; set; } = new List<VehiclePerformanceDto>();
        public List<DriverPerformanceDto> DriverPerformance { get; set; } = new List<DriverPerformanceDto>();
    }

    public class RecentTripDto
    {
        public int Id { get; set; }
        public string VehicleLicensePlate { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string StartLocation { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public decimal Distance { get; set; }
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string StartLocation { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string VehicleType { get; set; } = "";
        public int PassengerCount { get; set; }
    }

    public class RecentMaintenanceDto
    {
        public int Id { get; set; }
        public string VehicleLicensePlate { get; set; } = "";
        public string IssueDescription { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string ReportedBy { get; set; } = "";
    }

    public class VehiclePerformanceDto
    {
        public string LicensePlate { get; set; } = "";
        public string Model { get; set; } = "";
        public decimal TotalDistance { get; set; }
        public int TotalTrips { get; set; }
        public string Status { get; set; } = "";
    }

    public class DriverPerformanceDto
    {
        public string Name { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public decimal TotalDistance { get; set; }
        public int TotalTrips { get; set; }
        public string Status { get; set; } = "";
    }
} 