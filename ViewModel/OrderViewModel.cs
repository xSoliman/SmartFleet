using SmartFleet.Models;

namespace SmartFleet.ViewModel
{
    public class OrderViewModel
    {
        public IEnumerable<Order>? Orders { get; set; }
        
        // Original filters
        public string? SearchUserId { get; set; }
        public string? SearchStartLocation { get; set; }
        public string? SearchDestination { get; set; }
        public VehicleType? TypeFilter { get; set; }
        public OrderState? StateFilter { get; set; }
        
        // Date range filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public bool IsAdminUser { get; set; }
        public bool IsCommissioner { get; set; }
        public bool IsFleetManager { get; set; }
        public bool IsSysSupport { get; set; }
        public string? CurrentUserId { get; set; }
        public bool CanCreateOrder { get; set; }

        public Dictionary<int, string>? ResourceAvailability { get; set; } // OrderId -> "Available"/"Not Available"
    }
} 