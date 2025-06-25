namespace SmartFleet.ViewModel
{
    public class UserRoleManagementViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> CurrentRoles { get; set; } = new List<string>();
    }
} 