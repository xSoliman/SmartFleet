using SmartFleet.Models;
using System.Collections.Generic;

namespace SmartFleet.ViewModel
{
    public class OrderViewModel
    {
        public List<Order> Orders { get; set; }
        public bool IsAdminUser { get; set; }
        public bool IsFleetManager { get; set; }
        public bool IsCommissioner { get; set; }
        public bool IsSysSupport { get; set; }
        public bool IsNormalUser { get; set; }
        public string SearchKeyword { get; set; }
        public OrderState? StateFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Dictionary<int, string> ResourceAvailability { get; set; }
        public bool CanCreateOrder { get; set; }
        public string CurrentUserId { get; set; }
    }
} 