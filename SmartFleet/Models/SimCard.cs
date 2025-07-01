using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public enum SimCardStatus
    {
        Inactive = 0,
        Active = 1
    }

    public class SimCard
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SIM number is required")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "SIM number must be between 10 and 20 characters")]
        [Display(Name = "SIM Number")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "SIM number can only contain numbers")]
        public string SimNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Carrier is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Carrier must be between 2 and 50 characters")]
        [Display(Name = "Carrier")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Carrier can only contain letters and spaces")]
        public string Carrier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public SimCardStatus Status { get; set; } = SimCardStatus.Inactive;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
