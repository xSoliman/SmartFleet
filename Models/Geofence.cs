using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Models
{
    public enum GeofenceType
    {
        Circle = 0,
        Polygon = 1
    }

    public class Geofence
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Geofence name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [Display(Name = "Geofence Name")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Geofence type is required")]
        [Display(Name = "Geofence Type")]
        public GeofenceType Type { get; set; } = GeofenceType.Circle;
        
        // Circle properties
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        [Display(Name = "Center Latitude")]
        public decimal CenterLat { get; set; }
        
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        [Display(Name = "Center Longitude")]
        public decimal CenterLng { get; set; }
        
        [Range(1, 50000, ErrorMessage = "Radius must be between 1 and 50,000 meters")]
        [Display(Name = "Radius (meters)")]
        public decimal RadiusMeters { get; set; }
        
        // Polygon properties (nullable, JSON array of [lat, lng] pairs)
        [StringLength(10000, ErrorMessage = "Polygon JSON data is too large")]
        [Display(Name = "Polygon JSON Data")]
        public string? PolygonJson { get; set; }
        
        // Global default geofence flag
        [Display(Name = "Is Default Geofence")]
        public bool IsDefault { get; set; } = false;
    }
} 