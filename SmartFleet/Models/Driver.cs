namespace SmartFleet.Models
{
    public enum DriverState
    {
        Available,
        NotAvailable,
        AssignedOnScheduledTrip,
        OnTrip
    }
    public class Driver : ApplicationUser
    {
       
        
        public string LicenseNumber { get; set; }
        
        public DateTime LicenseExpiryDate { get; set; }
        public DriverState DriverStatus { get; set; }
        public int DrowsinessCount { get; set; } = 0;
        public new List<Trip>? Trips { get; set; }

    }
  
}
