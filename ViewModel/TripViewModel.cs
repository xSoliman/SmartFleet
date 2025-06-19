using SmartFleet.Models;
using System;
using System.Collections.Generic;

namespace SmartFleet.ViewModel
{
    public class TripViewModel
    {
        public IEnumerable<Trip>? Trips { get; set; }
        public string? SearchDriverName { get; set; }
        public VehicleType? VehicleType { get; set; }
        public string? Destination { get; set; }
        public TripState? StateFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
} 