using System.ComponentModel.DataAnnotations;
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
        [Range(1, int.MaxValue, ErrorMessage = "Invalid vehicle ID")]
        [Display(Name = "Vehicle")]
        public int? VehicleId { get; set; }
        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }
        [StringLength(450, MinimumLength = 1, ErrorMessage = "Reported by user ID is required")]
        [Display(Name = "Reported By")]
        public string? ReportedBy { get; set; }
        [ForeignKey("ReportedBy")]
        public ApplicationUser? ReportedUser { get; set; }
        [Required(ErrorMessage = "Issue description is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Issue description must be between 10 and 1000 characters")]
        [Display(Name = "Issue Description")]
        public string IssueDescription { get; set; } = string.Empty;
        [Required(ErrorMessage = "Repair status is required")]
        [Display(Name = "Repair Status")]
        public RepairState RepairStatus { get; set; }
        [Required(ErrorMessage = "Priority is required")]
        [Display(Name = "Priority")]
        public PriorityDegree Priority { get; set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Repaired At")]
        public DateTime? RepairedAt { get; set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
