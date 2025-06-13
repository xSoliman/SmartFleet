using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public enum TripState
    {
        [Display(Name = "Scheduled")]
        scheduled,

        [Display(Name = "In Progress")]
        in_progress,

        [Display(Name = "Completed")]
        completed,

        [Display(Name = "Cancelled")]
        cancelled
    }
    public class Trip
    {
        public int Id { get; set; }
        public int? VehicleId { get; set; }
        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }
        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
        public string? DriverId { get; set; }
        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public decimal Distance { get; set; }
        public TripState Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public ApplicationUser admin { get; set; } 

    }
}
