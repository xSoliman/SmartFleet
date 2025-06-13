namespace SmartFleet.Models
{
    public enum DriverState
    {
        active,
        inactive,
        on_trip
    }
    public class Driver : ApplicationUser
    {
       
        public string LicenseNumber { get; set; }
        public DateTime LicenseExpiryDate { get; set; }
        public DriverState DriverStatus { get; set; }
        public List<Trip>? Trips { get; set; }

    }
  
}
