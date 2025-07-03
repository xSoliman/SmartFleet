using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public class VehicleLocation
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Vehicle is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid vehicle ID")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }
        
        [ForeignKey("VehicleId")]
        public Vehicle Vehicle { get; set; } = null!;
        
        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        [Display(Name = "Latitude")]
        public decimal Latitude { get; set; }
        
        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        [Display(Name = "Longitude")]
        public decimal Longitude { get; set; }
        
        [Range(0, 500, ErrorMessage = "Speed must be between 0 and 500 km/h")]
        [Display(Name = "Speed (km/h)")]
        public decimal Speed { get; set; }
        
        [Required(ErrorMessage = "Timestamp is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; }
        
        [StringLength(100, ErrorMessage = "Device ID cannot exceed 100 characters")]
        [Display(Name = "Device ID")]
        public string? DeviceId { get; set; }      // Optional device identifier
        
        [StringLength(100, ErrorMessage = "Device model cannot exceed 100 characters")]
        [Display(Name = "Device Model")]
        public string? DeviceModel { get; set; }   // Optional device model
    }
}
