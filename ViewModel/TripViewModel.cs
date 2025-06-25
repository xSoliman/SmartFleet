using SmartFleet.Models;
using System;
using System.Collections.Generic;

namespace SmartFleet.ViewModel
{
    public class TripViewModel
    {
        public List<Trip>? Trips { get; set; }
        public List<Trip>? AssignedTrips { get; set; }
        public string? Destination { get; set; }
        public string? SearchDriverName { get; set; }
        public VehicleType? VehicleType { get; set; }
        public TripState? StateFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Filters for assigned trips (for drivers)
        public string? AssignedDestination { get; set; }
        public string? AssignedSearchDriverName { get; set; }
        public VehicleType? AssignedVehicleType { get; set; }
        public TripState? AssignedStateFilter { get; set; }
        public DateTime? AssignedStartDate { get; set; }
        public DateTime? AssignedEndDate { get; set; }
        
        // Role-based properties
        public bool IsDriver { get; set; }
        public bool IsFleetManager { get; set; }
        public bool IsSysSupport { get; set; }
        public bool IsNormalUser { get; set; }
        public string? CurrentUserId { get; set; }
    }
} 