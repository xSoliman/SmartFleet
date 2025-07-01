using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Models
{
    public enum DriverState
    {
        Available,
        NotAvailable,
        AssignedOnScheduledTrip,
        OnTrip
    }
    public class Driver : ApplicationUser
    {
        [Required(ErrorMessage = "License number is required")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "License number must be between 5 and 20 characters")]
        [Display(Name = "License Number")]
        [RegularExpression(@"^[A-Z0-9\s\-]+$", ErrorMessage = "License number can only contain uppercase letters, numbers, spaces, and hyphens")]
        public string LicenseNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "License expiry date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "License Expiry Date")]
        [FutureDate(ErrorMessage = "License expiry date must be in the future")]
        public DateTime LicenseExpiryDate { get; set; }
        
        [Required(ErrorMessage = "Driver status is required")]
        [Display(Name = "Driver Status")]
        public DriverState DriverStatus { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Drowsiness count cannot be negative")]
        [Display(Name = "Drowsiness Count")]
        public int DrowsinessCount { get; set; } = 0;
        
        public new List<Trip>? Trips { get; set; }
    }


}
