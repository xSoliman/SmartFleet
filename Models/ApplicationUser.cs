using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Default value is active account
        public bool AccountStatus { get; set; } = true;
        public string? ProfileImageUrl { get; set; } = "default";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<Event>? Events { get; set; }
        public List<Maintenance>? Maintenances { get; set; }
        public List<Notification>? Notifications { get; set; }
        public List<Order>? Orders { get; set; }
        public List<Trip>? Trips { get; set; }


    }
}
