namespace SmartFleet.Models
{
    
        public class FleetManagerViewModel
        {
            public ApplicationUser FleetManager { get; set; }
            public List<Order> PendingOrders { get; set; }
        }
   
}
