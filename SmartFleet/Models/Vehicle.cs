using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SmartFleet.Models
{
    public enum VehicleState
    {
        available,
        need_maintenance,
        under_maintenance,
        maintained,
        on_trip,
        on_scheduled_trip
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
        
        [Required(ErrorMessage = "Model is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Model must be between 2 and 100 characters")]
        [Display(Name = "Model")]
        public string Model { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vehicle type is required")]
        [Display(Name = "Vehicle Type")]
        public VehicleType Type { get; set; }
        
        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }
        
        [Display(Name = "Vehicle Image")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string? VehicleImageUrl { get; set; }
        
        [Required(ErrorMessage = "License plate is required")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "License plate must be between 5 and 20 characters")]
        [Display(Name = "License Plate")]
        [RegularExpression(@"^[A-Z0-9\s\-]+$", ErrorMessage = "License plate can only contain uppercase letters, numbers, spaces, and hyphens")]
        public string LicensePlate { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public VehicleState Status { get; set; }
        
        [Display(Name = "Total Distance Traveled (km)")]
        [Range(0, double.MaxValue, ErrorMessage = "Distance cannot be negative")]
        [Precision(12, 7)]
        public decimal TotalDistanceTraveled { get; set; } = 0;
        
        [Display(Name = "Registration Expiry Date")]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Registration expiry date must be in the future")]
        public DateTime? RegistrationExpiryDate { get; set; }
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public List<Trip>? Trips { get; set; }
        public List<Maintenance>? Maintenances { get; set; }
        public List<VehicleLocation>? VehicleLocations { get; set; }
        
        [ForeignKey("SimCardId")]
        public SimCard? SimCard { get; set; }
        public int? SimCardId { get; set; }
        public int? GeofenceId { get; set; }
        
        [ForeignKey("GeofenceId")]
        public Geofence? Geofence { get; set; }
        public bool? WasInsideGeofence { get; set; }
    }


}
