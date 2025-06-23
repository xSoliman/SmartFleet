using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Models
{
    public class Geofence
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        public decimal CenterLat { get; set; }
        [Required]
        public decimal CenterLng { get; set; }
        [Required]
        public decimal RadiusMeters { get; set; }
    }
} 