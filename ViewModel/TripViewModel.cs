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
        
        // Role-based properties
        public bool IsDriver { get; set; }
        public bool IsFleetManager { get; set; }
        public bool IsSystemSupport { get; set; }
        public string? CurrentUserId { get; set; }
    }
} 