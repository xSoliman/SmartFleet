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
        [Required]
        [StringLength(100)]
        [Display(Name = "Model")]
        public string Model { get; set; }
        [Required]
        [Display(Name = "Vehicle Type")]
        public VehicleType Type { get; set; }
        [Required]
        [Range(1, 100)]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }
        [Display(Name = "Vehicle Image")]
        public string? VehicleImageUrl { get; set; }
        [Required]
        [StringLength(20)]
        [Display(Name = "License Plate")]
        public string LicensePlate { get; set; }
        [Required]
        [Display(Name = "Status")]
        public VehicleState Status { get; set; }
        [Display(Name = "Total Distance Traveled (km)")]
        // This value is updated automatically from GPS data
         [Precision(12, 7)]

        public decimal TotalDistanceTraveled { get; set; }
        [Display(Name = "Registration Expiry Date")]
        [DataType(DataType.Date)]
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
