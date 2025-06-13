using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public RelatedTable RelatedTable { get; set; }
        public int? RelatedId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
