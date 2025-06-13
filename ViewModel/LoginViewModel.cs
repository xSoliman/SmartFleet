using System.ComponentModel.DataAnnotations;

namespace SmartFleet.ViewModel
{
    public class LoginViewModel
    {

        [DataType(DataType.EmailAddress), Display(Name = "Email Address")]
        public string Email { get; set; }

        [DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
