using SmartFleet.Models;

namespace SmartFleet.Services.Interfaces
{
    public interface IUserRoleService
    {
        Task<bool> HasAccessToVehicles(ApplicationUser user);
        Task<bool> HasAccessToDrivers(ApplicationUser user);
        Task<bool> HasAccessToMaintenance(ApplicationUser user);
        Task<bool> HasAccessToOrders(ApplicationUser user);
        Task<bool> HasAccessToTrips(ApplicationUser user);
        Task<bool> HasAccessToTracking(ApplicationUser user);
        Task<bool> HasAccessToDashboard(ApplicationUser user);
        Task<bool> HasAccessToReports(ApplicationUser user);
        Task<bool> CanCreateOrder(ApplicationUser user);
        Task<bool> CanEditOrder(ApplicationUser user, OrderState orderStatus);
        Task<bool> CanCancelOrder(ApplicationUser user, OrderState orderStatus);
        Task<bool> CanApproveRejectOrder(ApplicationUser user);
        Task<bool> CanCreateTrip(ApplicationUser user);
        Task<bool> CanEditTrip(ApplicationUser user);
        Task<bool> CanCancelTrip(ApplicationUser user, TripState tripStatus);
        Task<bool> CanCreateMaintenance(ApplicationUser user);
        Task<bool> CanEditMaintenance(ApplicationUser user);
        Task<List<string>> GetUserRoles(ApplicationUser user);
        Task<List<ApplicationUser>> GetUsersByRole(string roleName);
    }
}
