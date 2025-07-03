using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public enum EventType
    {
        Create,
        Update,
        Delete,
        UserAction,
        SystemAlert,
        MaintenanceScheduled,
        MaintenanceCompleted,
        TripAssigned,
        TripCompleted
    }

    public enum Severity
    {
        info,
        warning,
        error,
    }

    public enum RelatedTable
    {
        None,           // For cases where no related table is applicable
        User,           // Related to user actions or data
        Vehicle,        // Related to vehicle details or management
        Trip,           // Related to trip management or tracking
        Maintenance,    // Related to maintenance records or updates
        Order,          // Related to user orders
        Notification,   // Related to notifications
        Report,          // Related to reports or analytics
        SimCard,
        VehicleLocatoin,
        Driver
    }

    public class Event
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Event type is required")]
        [Display(Name = "Event Type")]
        public EventType Type { get; set; }
        
        [Required(ErrorMessage = "Severity is required")]
        [Display(Name = "Severity")]
        public Severity Severity { get; set; }
        
        [Required(ErrorMessage = "Related table is required")]
        [Display(Name = "Related Table")]
        public RelatedTable RelatedTable { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Related ID cannot be negative")]
        [Display(Name = "Related ID")]
        public int RelatedId { get; set; }
        
        [StringLength(450, ErrorMessage = "User ID cannot exceed 450 characters")]
        [Display(Name = "User")]
        public string? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
        
        [Required(ErrorMessage = "Message is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Message must be between 5 and 500 characters")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
