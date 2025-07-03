using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFleet.Models
{
    public enum OrderState
    {
        Pending,
        Approved,
        Cancelled,
        Rejected,
    }

    public class Order
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "User is required")]
        [StringLength(450, MinimumLength = 1, ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
        
        [Required(ErrorMessage = "Vehicle type is required")]
        [Display(Name = "Vehicle Type")]
        public VehicleType VehicleType { get; set; }
        
        [Required(ErrorMessage = "Passenger count is required")]
        [Range(1, 100, ErrorMessage = "Passenger count must be between 1 and 100")]
        [Display(Name = "Passenger Count")]
        public int PassengerCount { get; set; }
        
        [Required(ErrorMessage = "Start location is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Start location must be between 3 and 200 characters")]
        [Display(Name = "Start Location")]
        public string StartLocation { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Destination is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Destination must be between 3 and 200 characters")]
        [Display(Name = "Destination")]
        public string Destination { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Trip start date is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Trip Start Date")]
        [FutureDate(ErrorMessage = "Trip start date must be in the future")]
        public DateTime TripStartDate { get; set; }
        
        [Required(ErrorMessage = "Trip end date is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Trip End Date")]
        [FutureDate(ErrorMessage = "Trip end date must be in the future")]
        public DateTime TripEndDate { get; set; }
        
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 500 characters")]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Order status is required")]
        [Display(Name = "Status")]
        public OrderState Status { get; set; }
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Trip? Trip { get; set; }
    }


}
