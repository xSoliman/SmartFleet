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
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        public GeofenceType Type { get; set; } = GeofenceType.Circle;
        // Circle
        public decimal CenterLat { get; set; }
        public decimal CenterLng { get; set; }
        public decimal RadiusMeters { get; set; }
        // Polygon (nullable, JSON array of [lat, lng] pairs)
        public string? PolygonJson { get; set; }
        // Global default geofence flag
        public bool IsDefault { get; set; } = false;
    }
} 