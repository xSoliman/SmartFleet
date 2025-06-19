using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
public enum Roles
{
    NormalUser,
    SysSupport,
    FleetManager,
    MaintanceManager,
    commissioner,
    Driver,

}
namespace SmartFleet
{
    public class Dbinitializer
    {
        private readonly UserManager<ApplicationUser> _ApplicationUserManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SmartFleetContext _db;
        private readonly ILogger<Dbinitializer> _logger;

        public Dbinitializer(
            UserManager<ApplicationUser> ApplicationUserManager,
            RoleManager<IdentityRole> roleManager,
            SmartFleetContext db,
            ILogger<Dbinitializer> logger)
        {
            _ApplicationUserManager = ApplicationUserManager;
            _roleManager = roleManager;
            _db = db;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Check if database exists and has been migrated
                bool databaseExists = await _db.Database.CanConnectAsync();
                if (!databaseExists)
                {
                    _logger.LogInformation("Database does not exist. Creating and applying migrations...");
                }

                if (databaseExists && !(await _db.Database.GetPendingMigrationsAsync()).Any())
                {
                    _logger.LogInformation("No pending migrations. Skipping database initialization.");

                    // Check if roles exist to determine if initial data is seeded
                    if (await _roleManager.RoleExistsAsync("Driver"))
                    {
                        _logger.LogInformation("Roles already exist. Skipping seeding.");
                        return;
                    }
                }
                else
                {
                    await _db.Database.MigrateAsync();
                    _logger.LogInformation("Database migrations applied successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during database initialization.");
                return;
            }

            await SeedRolesAndAdminApplicationUserAsync();
            await SeedInitialDataAsync();
            _logger.LogInformation("Database initialization completed successfully.");
        }

        private async Task SeedRolesAndAdminApplicationUserAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Driver"))
            {
                List<string> roles = Enum.GetValues(typeof(Roles)).Cast<Roles>().Select(r => r.ToString()).ToList();

                foreach (var roleName in roles)
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role '{RoleName}': {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                // Create syssupport user
                var adminApplicationUser = new ApplicationUser
                {
                    UserName = "SmartFleet",
                    Email = "SmartFleet@Support.com",
                    EmailConfirmed = true
                };

                var createResult = await _ApplicationUserManager.CreateAsync(adminApplicationUser, "123456789k");

                if (createResult.Succeeded)
                {
                    var createdUser = await _ApplicationUserManager.FindByEmailAsync("SmartFleet@Support.com");
                    if (createdUser != null)
                    {
                        await _ApplicationUserManager.AddToRoleAsync(createdUser, "SysSupport");
                        _logger.LogInformation("Admin user created and assigned to SysSupport role successfully.");
                    }
                }
                else
                {
                    _logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task SeedInitialDataAsync()
        {
            try
            {
                // Check if admin user exists, if not create it
                if (!await _db.Users.AnyAsync(u => u.Email == "admin@smartfleet.com"))
                {
                    // Seed ApplicationUser
                    var adminUser = new ApplicationUser
                    {
                        Id = "1",
                        UserName = "admin@smartfleet.com",
                        Email = "admin@smartfleet.com",
                        ProfileImageUrl = "https://example.com/admin.jpg",
                        AccountStatus = true,
                        CreatedAt = DateTime.Now,
                        EmailConfirmed = true
                    };

                    var adminResult = await _ApplicationUserManager.CreateAsync(adminUser, "Password123!");
                    if (adminResult.Succeeded)
                    {
                        await _ApplicationUserManager.AddToRoleAsync(adminUser, "FleetManager");
                        _logger.LogInformation("Admin user seeded successfully.");
                    }
                    else
                    {
                        _logger.LogError("Failed to seed admin user: {Errors}",
                            string.Join(", ", adminResult.Errors.Select(e => e.Description)));
                    }
                }

                // Check if driver user exists, if not create it
                if (!await _db.Users.AnyAsync(u => u.Email == "driver@smartfleet.com"))
                {
                    // Seed Driver
                    var driverUser = new Driver
                    {
                        Id = "2",
                        UserName = "driver@smartfleet.com",
                        Email = "driver@smartfleet.com",
                        LicenseNumber = "AB12345",
                        LicenseExpiryDate = DateTime.Now.AddYears(2),
                        DriverStatus = DriverState.active,
                        ProfileImageUrl = "https://example.com/driver.jpg",
                        CreatedAt = DateTime.Now,
                        EmailConfirmed = true
                    };

                    var driverResult = await _ApplicationUserManager.CreateAsync(driverUser, "Password123!");
                    if (driverResult.Succeeded)
                    {
                        await _ApplicationUserManager.AddToRoleAsync(driverUser, "Driver");
                        _logger.LogInformation("Driver user seeded successfully.");
                    }
                    else
                    {
                        _logger.LogError("Failed to seed driver user: {Errors}",
                            string.Join(", ", driverResult.Errors.Select(e => e.Description)));
                    }
                }

                // Seed Commissioner (this is the main purpose of this update)
                var commissionerUser = new ApplicationUser
                {
                    Id = "3",
                    UserName = "commissioner@smartfleet.com",
                    Email = "commissioner@smartfleet.com",
                    ProfileImageUrl = "https://example.com/commissioner.jpg",
                    AccountStatus = true,
                    CreatedAt = DateTime.Now,
                    EmailConfirmed = true
                };

                var commissionerResult = await _ApplicationUserManager.CreateAsync(commissionerUser, "Password123!");
                if (commissionerResult.Succeeded)
                {
                    await _ApplicationUserManager.AddToRoleAsync(commissionerUser, "commissioner");
                    _logger.LogInformation("Commissioner user seeded successfully.");
                }
                else
                {
                    _logger.LogError("Failed to seed commissioner user: {Errors}",
                        string.Join(", ", commissionerResult.Errors.Select(e => e.Description)));
                }

                // Seed Maintenance Manager
                var maintenanceManagerUser = new ApplicationUser
                {
                    Id = "4",
                    UserName = "maintenance@smartfleet.com",
                    Email = "maintenance@smartfleet.com",
                    ProfileImageUrl = "https://example.com/maintenance.jpg",
                    AccountStatus = true,
                    CreatedAt = DateTime.Now,
                    EmailConfirmed = true
                };

                var maintenanceManagerResult = await _ApplicationUserManager.CreateAsync(maintenanceManagerUser, "Password123!");
                if (maintenanceManagerResult.Succeeded)
                {
                    await _ApplicationUserManager.AddToRoleAsync(maintenanceManagerUser, "MaintanceManager");
                    _logger.LogInformation("Maintenance Manager user seeded successfully.");
                }
                else
                {
                    _logger.LogError("Failed to seed maintenance manager user: {Errors}",
                        string.Join(", ", maintenanceManagerResult.Errors.Select(e => e.Description)));
                }

                // Only seed other data if it doesn't already exist
                if (!await _db.Vehicles.AnyAsync())
                {
                    // Seed Vehicles
                    var vehicles = new[]
                    {
                        new Vehicle
                        {
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
                            Model = "Ford Transit",
                            Type = VehicleType.Van,
                            Capacity = 12,
                            LicensePlate = "XYZ 5678",
                            Status = VehicleState.available,
                            Distance = 500,
                            VehicleImageUrl = "https://example.com/ford.jpg",
                            CreatedAt = DateTime.Now
                        }
                    };

                    await _db.Vehicles.AddRangeAsync(vehicles);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Vehicles seeded successfully.");

                    // Get the vehicle IDs after saving
                    var toyotaVehicle = await _db.Vehicles.FirstAsync(v => v.LicensePlate == "XYZ 1234");

                    // Seed SimCard
                    var simCard = new SimCard
                    {
                        VehicleId = toyotaVehicle.Id,
                        SimNumber = "1234567890",
                        Carrier = "CarrierX",
                        ActivatedAt = DateTime.Now,
                        Status = SimCardStatus.Active,
                        CreatedAt = DateTime.Now
                    };

                    await _db.SimCards.AddAsync(simCard);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("SimCard seeded successfully.");

                    // Seed Maintenance
                    var maintenance = new Maintenance
                    {
                        VehicleId = toyotaVehicle.Id,
                        ReportedBy = "1",
                        IssueDescription = "Flat tire",
                        RepairStatus = RepairState.pending,
                        Priority = PriorityDegree.high,
                        CreatedAt = DateTime.Now
                    };

                    await _db.Maintenances.AddAsync(maintenance);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Maintenance record seeded successfully.");

                    // Seed Order
                    var order = new Order
                    {
                        UserId = "1",
                        VehicleType = VehicleType.Car,
                        PassengerCount = 3,
                        StartLocation = "University",
                        Destination = "Airport",
                        TripStartDate = DateTime.Now.AddHours(1),
                        TripEndDate = DateTime.Now.AddHours(3),
                        Reason = "Business Trip",
                        Status = OrderState.Pending,
                        CreatedAt = DateTime.Now
                    };

                    await _db.Orders.AddAsync(order);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Order seeded successfully.");

                    // Seed Trip
                    var trip = new Trip
                    {
                        VehicleId = toyotaVehicle.Id,
                        OrderId = order.Id,
                        DriverId = "2",
                        StartTime = DateTime.Now.AddHours(1),
                        EndTime = DateTime.Now.AddHours(3),
                        Distance = 50,
                        Status = TripState.Scheduled,
                        CreatedAt = DateTime.Now,
                        CreatedBy = "1"
                    };

                    await _db.Trips.AddAsync(trip);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Trip seeded successfully.");
                }
                else
                {
                    _logger.LogInformation("Other data already exists. Skipping vehicle and related data seeding.");
                }

                _logger.LogInformation("Commissioner user and other missing data seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding initial data.");
            }
        }
    }
}