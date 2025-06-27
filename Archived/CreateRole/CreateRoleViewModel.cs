using System.ComponentModel.DataAnnotations;

namespace SmartFleet.ViewModel
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Role name is required")]
        [Display(Name = "Role Name")]
        [StringLength(50, ErrorMessage = "Role name must be less than 50 characters")]
        public string RoleName { get; set; } = string.Empty;

        [Display(Name = "Role Description")]
        [StringLength(200, ErrorMessage = "Description must be less than 200 characters")]
        public string? Description { get; set; }
    }
} 