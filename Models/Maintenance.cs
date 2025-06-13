using System.ComponentModel.DataAnnotations.Schema;


namespace SmartFleet.Models
{
    public enum RepairState
    {
        pending,
        in_progress,
        completed
    }
    public enum PriorityDegree
    {
        low,
        normal,
        high
    }
    public class Maintenance
    {
        public int Id { get; set; }
        public int? VehicleId { get; set; }
        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }
        public string? ReportedBy { get; set; }
        [ForeignKey("ReportedBy")]
        public ApplicationUser? ReportedUser { get; set; }
        public string IssueDescription { get; set; }
        public RepairState RepairStatus { get; set; }
        public PriorityDegree Priority { get; set; }
        public DateTime? RepairedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
