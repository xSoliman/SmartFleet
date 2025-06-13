using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public enum OrderState
    {
        pending,
        in_progress,
        approved,
        cancelled,
        rejected,
        NULLL
    }

    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public VehicleType VehicleType { get; set; }
        public int PassengerCount { get; set; }
        public string TripStartLocation { get; set; }
        public string TripEndLocation { get; set; }
        public DateTime TripStartDate { get; set; }
        public DateTime TripEndDate { get; set; }
        public string Reason { get; set; }
        public OrderState Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Trip Trip { get; set; }

    }
}
