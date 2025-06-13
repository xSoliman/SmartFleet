using Microsoft.AspNetCore.Http;
using SmartFleet.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartFleet.ViewModel
{
    public class DriverViewModel
    {
        public string? Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        // On creation this field is required.
        // On edit, if you wish to change the password, fill in this field.
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required]
        public string LicenseNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "License Expiry Date")]
        [Required]
        public DateTime LicenseExpiryDate { get; set; }

        [Required]
        [Display(Name = "Driver Status")]
        public DriverState DriverStatus { get; set; }

        // For the profile image upload.
        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImageFile { get; set; }
    }
}
