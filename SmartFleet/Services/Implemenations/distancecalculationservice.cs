using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
   

    public class DistanceCalculationService : IDistanceCalculationService
    {
        /// <summary>
        /// Calculates the distance between two GPS coordinates using the Haversine formula
        /// </summary>
        /// <param name="lat1">Latitude of first point</param>
        /// <param name="lon1">Longitude of first point</param>
        /// <param name="lat2">Latitude of second point</param>
        /// <param name="lon2">Longitude of second point</param>
        /// <returns>Distance in kilometers</returns>
        public decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double earthRadius = 6371; // Earth's radius in kilometers

            // Convert to radians
            var lat1Rad = (double)lat1 * Math.PI / 180;
            var lon1Rad = (double)lon1 * Math.PI / 180;
            var lat2Rad = (double)lat2 * Math.PI / 180;
            var lon2Rad = (double)lon2 * Math.PI / 180;

            // Haversine formula
            var dLat = lat2Rad - lat1Rad;
            var dLon = lon2Rad - lon1Rad;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = (decimal)(earthRadius * c);

            return Math.Round(distance, 3); // Round to 3 decimal places
        }

        /// <summary>
        /// Calculates the total distance for a trip based on GPS tracking data
        /// </summary>
        /// <param name="tripId">The trip ID</param>
        /// <param name="context">Database context</param>
        /// <returns>Total distance in kilometers</returns>
        public decimal CalculateTripDistance(int tripId, Data.SmartFleetContext context)
        {
            var trip = context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Order)
                .FirstOrDefault(t => t.Id == tripId);

            if (trip == null || trip.Vehicle == null)
                return 0;

            // Get all GPS locations for this vehicle during the trip period
            var tripStartTime = trip.Order.TripStartDate;
            var tripEndTime = trip.Order.TripEndDate;

            var locations = context.VehicleLocations
                .Where(vl => vl.VehicleId == trip.VehicleId &&
                            vl.Timestamp >= tripStartTime &&
                            vl.Timestamp <= tripEndTime)
                .OrderBy(vl => vl.Timestamp)
                .ToList();

            if (locations.Count < 2)
                return 0;

            decimal totalDistance = 0;

            // Calculate distance between consecutive GPS points
            for (int i = 1; i < locations.Count; i++)
            {
                var prevLocation = locations[i - 1];
                var currentLocation = locations[i];

                // Only calculate distance if the vehicle is moving (speed > 0)
                if (currentLocation.Speed > 0)
                {
                    var segmentDistance = CalculateDistance(
                        prevLocation.Latitude, prevLocation.Longitude,
                        currentLocation.Latitude, currentLocation.Longitude
                    );
                    totalDistance += segmentDistance;
                }
            }

            return Math.Round(totalDistance, 3);
        }
    }
} 