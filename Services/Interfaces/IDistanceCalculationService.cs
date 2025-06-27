namespace SmartFleet.Services.Interfaces
{
    public interface IDistanceCalculationService
    {
        decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
        decimal CalculateTripDistance(int tripId, SmartFleet.Data.SmartFleetContext context);
    }
}
