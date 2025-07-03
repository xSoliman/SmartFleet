using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "User is required")]
        [StringLength(450, MinimumLength = 1, ErrorMessage = "User ID is required")]
        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Message is required")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Message must be between 5 and 1000 characters")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Related table is required")]
        [Display(Name = "Related Table")]
        public RelatedTable RelatedTable { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Related ID must be positive")]
        [Display(Name = "Related ID")]
        public int? RelatedId { get; set; }
        
        [Display(Name = "Is Read")]
        public bool IsRead { get; set; }
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
