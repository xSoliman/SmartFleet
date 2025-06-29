using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SmartFleet.Models;

namespace SmartFleet.ViewModel
{
    public class EditProfileViewModel
    {
        [Display(Name = "First Name"), Required, DataType(DataType.Text),
         MinLength(3), MaxLength(50), RegularExpression(@"^[a-zA-Z]+$",
         ErrorMessage = "The First name must contain only letters")]
        public string UserName { get; set; }

        [Display(Name = "Email Address"), Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DataType(DataType.PhoneNumber), Display(Name = "Phone Number"), Required]
        [StringLength(11, ErrorMessage = "Phone number must be exactly 11 digits.")]
        [RegularExpression(@"01[0-2]\d{8}|015\d{8}", ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }

        // Make the image optional
        [Display(Name = "Profile Picture")]
        public IFormFile? ImageFile { get; set; } // Nullable (optional)

        public string? ImageUrl { get; set; } // Optional URL for displaying existing images
        
        [Display(Name = "Remove Profile Picture")]
        public bool RemoveImage { get; set; } // Checkbox for removing image
        
        // Driver-specific properties
        [Display(Name = "License Number")]
        [StringLength(20, ErrorMessage = "License number must not exceed 20 characters.")]
        public string? LicenseNumber { get; set; }
        
        [Display(Name = "License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }
        
        public DriverState? DriverStatus { get; set; }
        public bool IsDriver { get; set; }
    }
}
