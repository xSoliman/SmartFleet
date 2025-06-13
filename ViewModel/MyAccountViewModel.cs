using SmartFleet.Models;

namespace SmartFleet.ViewModel
{
    public class MyAccountViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public string ImageUrl { get; set; }
        public OrderState? OrderStatus
        {
            get; set;
            // URL of the user's profile picture
        }
    }
}
