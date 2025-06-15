using System;
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

        public int? VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }

        public string SimNumber { get; set; }

        public string Carrier { get; set; }

        public DateTime ActivatedAt { get; set; }

        public SimCardStatus Status { get; set; } = SimCardStatus.Inactive;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
