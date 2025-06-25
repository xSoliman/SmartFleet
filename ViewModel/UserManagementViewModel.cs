using SmartFleet.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartFleet.ViewModel
{
    // ViewModel للمستخدم الواحد
    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        
        // Driver-specific properties
        public bool IsDriver { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public DriverState? DriverStatus { get; set; }
    }

    // ViewModel الرئيسي لصفحة إدارة المستخدمين
    public class UserManagementViewModel
    {
        // إحصائيات
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int DriversCount { get; set; }
        
        // قائمة المستخدمين
        public List<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();
        
        // أدوار المستخدمين - Dictionary يربط ID المستخدم بأدواره
        public Dictionary<string, List<string>> UserRoles { get; set; } = new Dictionary<string, List<string>>();
        
        // تفاصيل السائقين - Dictionary يربط ID المستخدم بتفاصيل السائق
        public Dictionary<string, DriverDetailsViewModel> DriverDetails { get; set; } = new Dictionary<string, DriverDetailsViewModel>();
    }
    
    // ViewModel لتفاصيل السائق
    public class DriverDetailsViewModel
    {
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime? LicenseExpiryDate { get; set; }
        public DriverState DriverStatus { get; set; }
    }
} 