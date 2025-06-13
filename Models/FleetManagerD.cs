namespace SmartFleet.Models
{
    public class FleetManagerD
    {
        public ApplicationUser FleetManager { get; set; }
        public List<Order> PendingOrders { get; set; }
        public List<Driver> AvailableDrivers { get; set; } // List of drivers
        public string SelectedDriverId { get; set; } // Selected driver ID

    }
}
