using System.ComponentModel.DataAnnotations;

namespace SmartFleet.ViewModel
{
    public class RoleViewModel
    {
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }
    }
}
