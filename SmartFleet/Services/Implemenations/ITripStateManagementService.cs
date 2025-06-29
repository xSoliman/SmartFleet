namespace SmartFleet.Services.Implemenations
{
    public interface ITripStateManagementService
    {
        Task UpdateTripStatesAsync();
        Task UpdateSingleTripStateAsync(int tripId);
    }
}
