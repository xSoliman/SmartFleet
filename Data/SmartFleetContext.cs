using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SmartFleet.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmartFleet.Data
{
    public class SmartFleetContext : IdentityDbContext<ApplicationUser>
    {
        public SmartFleetContext() : base()
        {
        }

        public SmartFleetContext(DbContextOptions<SmartFleetContext> options) : base(options)
        {
        }

        public virtual new DbSet<ApplicationUser> Users { get; set; }
        public virtual DbSet<Vehicle> Vehicles { get; set; }
        public virtual DbSet<SimCard> SimCards { get; set; }
        public virtual DbSet<VehicleLocation> VehicleLocations { get; set; }
        public virtual DbSet<Driver> Drivers { get; set; }
        public virtual DbSet<Maintenance> Maintenances { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Trip> Trips { get; set; }
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<Geofence> Geofences { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VehicleLocation>()
            .Property(vl => vl.Latitude)
            .HasColumnType("decimal(9, 6)");

            modelBuilder.Entity<VehicleLocation>()
            .Property(vl => vl.Longitude)
            .HasColumnType("decimal(9, 6)");

            modelBuilder.Entity<Trip>()
            .Property(t => t.Distance)
            .HasColumnType("decimal(9, 6)");

            modelBuilder.Entity<Vehicle>()
            .Property(v => v.TotalDistanceTraveled)
            .HasColumnType("decimal(9, 6)");

            modelBuilder.Entity<Driver>().ToTable("Drivers");
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");

            // DriverState Enum Conversion
            modelBuilder.Entity<Driver>()
                .Property(d => d.DriverStatus)
                .HasConversion(
                    new EnumToStringConverter<DriverState>()
                );

            // Type Enum Conversion
            modelBuilder.Entity<Event>()
                .Property(e => e.Type)
                .HasConversion(
                    new EnumToStringConverter<EventType>()
                );

            // Severity Enum Conversion
            modelBuilder.Entity<Event>()
                .Property(e => e.Severity)
                .HasConversion(
                    new EnumToStringConverter<Severity>()
                );

            // RelatedTable Enum Conversion
            modelBuilder.Entity<Event>()
                .Property(e => e.RelatedTable)
                .HasConversion(
                    new EnumToStringConverter<RelatedTable>()
                );

            // RepairState Enum Conversion
            modelBuilder.Entity<Maintenance>()
                .Property(m => m.RepairStatus)
                .HasConversion(
                    new EnumToStringConverter<RepairState>()
                );

            // PriorityDegree Enum Conversion
            modelBuilder.Entity<Maintenance>()
                .Property(m => m.Priority)
                .HasConversion(
                    new EnumToStringConverter<PriorityDegree>()
                );

            // OrderState Enum Conversion
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion(
                    new EnumToStringConverter<OrderState>()
                );

            // TripState Enum Conversion
            modelBuilder.Entity<Trip>()
                .Property(t => t.Status)
                .HasConversion(
                    new EnumToStringConverter<TripState>()
                );

            // VehicleState Enum Conversion
            modelBuilder.Entity<Vehicle>()
                .Property(v => v.Status)
                .HasConversion(
                    new EnumToStringConverter<VehicleState>()
                );

            // VehicleType Enum Conversion
            modelBuilder.Entity<Vehicle>()
                .Property(v => v.Type)
                .HasConversion(
                    new EnumToStringConverter<VehicleType>()
                );

            modelBuilder.Entity<VehicleLocation>()
            .HasOne(vl => vl.Vehicle)
            .WithMany(v => v.VehicleLocations)
            .HasForeignKey(vl => vl.VehicleId);


            // Fix cascade delete conflicts for Trips
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Order)
                .WithOne(o => o.Trip)
                .HasForeignKey<Trip>(t => t.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Geofence configurations
            modelBuilder.Entity<Geofence>()
                .Property(g => g.CenterLat)
                .HasColumnType("decimal(9, 6)");

            modelBuilder.Entity<Geofence>()
                .Property(g => g.CenterLng)
                .HasColumnType("decimal(9, 6)");

            modelBuilder.Entity<Geofence>()
                .Property(g => g.RadiusMeters)
                .HasColumnType("decimal(18, 2)");

            // GeofenceType Enum Conversion
            modelBuilder.Entity<Geofence>()
                .Property(g => g.Type)
                .HasConversion(
                    new EnumToStringConverter<GeofenceType>()
                );
        }
    }
}