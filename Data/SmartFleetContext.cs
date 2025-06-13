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



        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<SimCard> SimCards { get; set; }
        public DbSet<VehicleLocation> VehicleLocations { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Maintenance> Maintenances { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Notification> Notifications { get; set; }

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
            .Property(v => v.Distance)
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

            // Seeding fake data

            // Seed ApplicationUser (default for all users)
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = "1",
                    UserName = "admin@smartfleet.com",
                    Email = "admin@smartfleet.com",
                    ProfileImageUrl = "https://example.com/admin.jpg",
                    AccountStatus = true,
                    CreatedAt = DateTime.Now
                }
            );
            

            // Seed Driver (inherits from ApplicationUser)
            modelBuilder.Entity<Driver>().HasData(
                new Driver
                {
                    Id = "2",
                    UserName = "driver@smartfleet.com",
                    Email = "driver@smartfleet.com",
                    LicenseNumber = "AB12345",
                    LicenseExpiryDate = DateTime.Now.AddYears(2),
                    DriverStatus = DriverState.active,
                    ProfileImageUrl = "https://example.com/driver.jpg",
                    CreatedAt = DateTime.Now
                }
            );
            

            // Seed Vehicle
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle
                {
                    Id = 1,
                    Model = "Toyota Corolla",
                    Type = VehicleType.Car,
                    Capacity = 5,
                    LicensePlate = "XYZ 1234",
                    Status = VehicleState.available,
                    Distance = 0,
                    VehicleImageUrl = "https://example.com/toyota.jpg",
                    CreatedAt = DateTime.Now
                },
                new Vehicle
                {
                    Id = 2,
                    Model = "Ford Transit",
                    Type = VehicleType.Van,
                    Capacity = 12,
                    LicensePlate = "XYZ 5678",
                    Status = VehicleState.available,
                    Distance = 500,
                    VehicleImageUrl = "https://example.com/ford.jpg",
                    CreatedAt = DateTime.Now
                }
            );

            // Seed Maintenance
            modelBuilder.Entity<Maintenance>().HasData(
                new Maintenance
                {
                    Id = 1,
                    VehicleId = 1,
                    ReportedBy = "1",
                    IssueDescription = "Flat tire",
                    RepairStatus = RepairState.pending,
                    Priority = PriorityDegree.high,
                    CreatedAt = DateTime.Now
                }
            );

            // Seed Order
            modelBuilder.Entity<Order>().HasData(
                new Order
                {
                    Id = 1,
                    UserId = "1",
                    VehicleType = VehicleType.Car,
                    PassengerCount = 3,
                    TripStartLocation = "University",
                    TripEndLocation = "Airport",
                    TripStartDate = DateTime.Now.AddHours(1),
                    TripEndDate = DateTime.Now.AddHours(3),
                    Reason = "Business Trip",
                    Status = OrderState.pending,
                    CreatedAt = DateTime.Now
                }
            );

            // Seed SimCard
            modelBuilder.Entity<SimCard>().HasData(
                new SimCard
                {
                    Id = 1,
                    VehicleId = 1,
                    SimNumber = "1234567890",
                    Carrier = "CarrierX",
                    ActivatedAt = DateTime.Now,
                    Status = true,
                    CreatedAt = DateTime.Now
                }
            );

            // Seed Trip
            modelBuilder.Entity<Trip>().HasData(
                new Trip
                {
                    Id = 1,
                    VehicleId = 1,
                    OrderId = 1,
                    DriverId = "2",
                    StartTime = DateTime.Now.AddHours(1),
                    StartLocation = "University",
                    EndLocation = "Airport",
                    Distance = 50,
                    Status = TripState.scheduled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "1"
                }
            );

        }

    }
}
