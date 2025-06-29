using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public enum TripState
    {
        [Display(Name = "Scheduled")]
        Scheduled,
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Completed")]
        Completed,
        [Display(Name = "Cancelled")]
        Cancelled
    }

    public class Trip
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public string DriverId { get; set; }
        public Driver Driver { get; set; }

        // Auto-calculated distance based on GPS tracking during trip
        public decimal Distance { get; set; } = 0;

        [Required]
        public TripState Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public ApplicationUser CreatedByUser { get; set; }
    }
}
