namespace SmartFleet.Models
{
    public enum VehicleState
    {
        available,
        in_use,
        under_maintenance
    }
    public enum VehicleType
    {
        Car,
        Truck,
        Bus,
        Van,
        Motorcycle,
        Other
    }
    public class Vehicle
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string? VehicleImageUrl { get; set; }
        public string LicensePlate { get; set; }
        public VehicleState Status { get; set; }
        public decimal Distance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<Trip>? Trips { get; set; }
        public List<Maintenance>? Maintenances { get; set; }
        public List<VehicleLocation>? VehicleLocations { get; set; }

        public SimCard? SimCard { get; set; }



    }
}
