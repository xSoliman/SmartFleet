using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
      public class SimCard
    {
        public int Id { get; set; }
        public int? VehicleId { get; set; }
        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }
        public string SimNumber { get; set; }
        public string Carrier { get; set; }
        public DateTime ActivatedAt { get; set; }
        public bool Status { get; set; } = false; //active/not active
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
    