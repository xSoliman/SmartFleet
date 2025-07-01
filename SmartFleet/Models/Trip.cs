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

        [Required(ErrorMessage = "Vehicle is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid vehicle ID")]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        [Required(ErrorMessage = "Order is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid order ID")]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [Required(ErrorMessage = "Driver is required")]
        [StringLength(450, MinimumLength = 1, ErrorMessage = "Driver ID is required")]
        public string DriverId { get; set; } = string.Empty;
        public Driver Driver { get; set; } = null!;

        // Auto-calculated distance based on GPS tracking during trip
        [Range(0, double.MaxValue, ErrorMessage = "Distance cannot be negative")]
        public decimal Distance { get; set; } = 0;

        [Required(ErrorMessage = "Trip status is required")]
        public TripState Status { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Created by user is required")]
        [StringLength(450, MinimumLength = 1, ErrorMessage = "Created by user ID is required")]
        public string CreatedBy { get; set; } = string.Empty;
        
        [ForeignKey("CreatedBy")]
        public ApplicationUser CreatedByUser { get; set; } = null!;
    }
}
