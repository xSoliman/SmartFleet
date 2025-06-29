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

        [Required]
        [StringLength(20)]
        [Display(Name = "SIM Number")]
        public string SimNumber { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Carrier")]
        public string Carrier { get; set; }

        [Display(Name = "Status")]
        public SimCardStatus Status { get; set; } = SimCardStatus.Inactive;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
