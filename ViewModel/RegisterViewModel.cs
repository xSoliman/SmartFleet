using System.ComponentModel.DataAnnotations;

namespace SmartFleet.ViewModel
{

/*    public enum UserType
    {
        Student,
        Instructor
    }*/
    public class RegisterViewModel
    {
/*        [Display(Name = "Type User"), Required]
        public UserType UserType { get; set; }*/

        [Display(Name = "First Name"), Required, DataType(DataType.Text),
         MinLength(3), MaxLength(50), RegularExpression(@"^[a-zA-Z]+$",
         ErrorMessage = "The First name must contain only letters")]
        public string UserName { get; set; }

        [Display(Name = "Email Address"), Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "Password"), Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password"), Display(Name = "Confirm Password"), Required]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [DataType(DataType.PhoneNumber), Display(Name = "Phone Number"), Required]
        [StringLength(11, ErrorMessage = "Phone number must be exactly 11 digits.")]
        [RegularExpression(@"01[0-2]\d{8}|015\d{8}", ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }
    }
}
