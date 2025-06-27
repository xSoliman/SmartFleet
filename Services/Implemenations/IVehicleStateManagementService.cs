namespace SmartFleet.Services.Implemenations
{
    public interface IVehicleStateManagementService
    {
        Task UpdateVehicleStatesAsync();
        Task UpdateSingleVehicleStateAsync(int vehicleId);
        Task UpdateVehicleStateOnTripAssignmentAsync(int vehicleId);
        Task UpdateVehicleStateOnTripCompletionAsync(int vehicleId);
    }
}
