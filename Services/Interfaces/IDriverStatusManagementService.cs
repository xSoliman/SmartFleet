namespace SmartFleet.Services.Interfaces
{
    public interface IDriverStatusManagementService
    {
        Task UpdateDriverStatusesAsync();
        Task UpdateSingleDriverStatusAsync(string driverId);
        Task UpdateDriverStatusOnTripAssignmentAsync(string driverId);
        Task UpdateDriverStatusOnTripCompletionAsync(string driverId);
    }

}
