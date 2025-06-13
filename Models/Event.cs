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
        public EventType Type { get; set; }
        public Severity Severity { get; set; }
        public RelatedTable RelatedTable { get; set; }
        public int RelatedId { get; set; }
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
