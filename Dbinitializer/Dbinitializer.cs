using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;

public enum Roles
{
    NormalUser,
    SysSupport,
    FleetManager,
    MaintenanceManager,
    commissioner,
    Driver
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
                        await _ApplicationUserManager.AddToRoleAsync(createdUser,Roles.SysSupport.ToString());
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
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
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
                            DriverStatus = DriverState.Available,
                            ProfileImageUrl = "https://example.com/driver.jpg",
                            AccountStatus = true,
                            CreatedAt = DateTime.Now,
                            EmailConfirmed = true
                        };

                        var driverResult = await _ApplicationUserManager.CreateAsync(driverUser, "Password123!");
                        if (driverResult.Succeeded)
                        {
                            await _ApplicationUserManager.AddToRoleAsync(driverUser, Roles.Driver.ToString());
                            _logger.LogInformation("Driver user seeded successfully.");
                        }
                        else
                        {
                            _logger.LogError("Failed to seed driver user: {Errors}",
                                string.Join(", ", driverResult.Errors.Select(e => e.Description)));
                        }
                    }


                    var fleetManagerUser = new ApplicationUser
                    {
                        Id = "43",
                        UserName = "fleetmanager@smartfleet.com",
                        Email = "fleetmanager@smartfleet.com",
                        AccountStatus = true,
                        CreatedAt = DateTime.Now,
                        EmailConfirmed = true
                    };
                    var fleetManagerResult = await _ApplicationUserManager.CreateAsync(fleetManagerUser, "Password123!");
                    if (fleetManagerResult.Succeeded)
                    {
                        await _ApplicationUserManager.AddToRoleAsync(fleetManagerUser, Roles.FleetManager.ToString());
                        _logger.LogInformation("Fleet Manager user seeded successfully.");
                    }
                    else
                    {
                        _logger.LogError("Failed to seed fleet manager user: {Errors}",
                            string.Join(", ", fleetManagerResult.Errors.Select(e => e.Description)));
                    }

                    // Seed Commissioner
                    var commissionerUser = new ApplicationUser
                    {
                        Id = "3",
                        UserName = "commissioner@smartfleet.com",
                        Email = "commissioner@smartfleet.com",
                        AccountStatus = true,
                        CreatedAt = DateTime.Now,
                        EmailConfirmed = true
                    };

                    var commissionerResult = await _ApplicationUserManager.CreateAsync(commissionerUser, "Password123!");
                    if (commissionerResult.Succeeded)
                    {
                        await _ApplicationUserManager.AddToRoleAsync(commissionerUser, Roles.commissioner.ToString());
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
                        AccountStatus = true,
                        CreatedAt = DateTime.Now,
                        EmailConfirmed = true
                    };

                    var maintenanceManagerResult = await _ApplicationUserManager.CreateAsync(maintenanceManagerUser, "Password123!");
                    if (maintenanceManagerResult.Succeeded)
                    {
                        await _ApplicationUserManager.AddToRoleAsync(maintenanceManagerUser, Roles.MaintenanceManager.ToString());
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
                        // 1. Seed 20 NormalUser users
                        var normalUsers = new List<ApplicationUser>();
                        for (int i = 1; i <= 20; i++)
                        {
                            var user = new ApplicationUser
                            {
                                UserName = $"normaluser{i}@smartfleet.com",
                                Email = $"normaluser{i}@smartfleet.com",
                                AccountStatus = true,
                                CreatedAt = DateTime.Now.AddMinutes(-i),
                                EmailConfirmed = true
                            };
                            var result = await _ApplicationUserManager.CreateAsync(user, "Password123!");
                            if (result.Succeeded)
                            {
                                await _ApplicationUserManager.AddToRoleAsync(user, Roles.NormalUser.ToString());
                                normalUsers.Add(user);
                            }
                            else
                            {
                                _logger.LogError($"Failed to create normal user {i}: {{0}}", string.Join(", ", result.Errors.Select(e => e.Description)));
                            }
                        }
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("20 NormalUser users seeded successfully.");

                        // 2. Seed 20 Vehicles
                        var vehicles = new List<Vehicle>();
                        for (int i = 1; i <= 20; i++)
                        {
                            vehicles.Add(new Vehicle
                            {
                                Model = $"Model-{i}",
                                Type = (VehicleType)(i % Enum.GetValues(typeof(VehicleType)).Length),
                                Capacity = 4 + (i % 5),
                                LicensePlate = $"XYZ-{1000 + i}",
                                Status = VehicleState.available,
                                TotalDistanceTraveled = (decimal)(i * 10.5),
                                RegistrationExpiryDate = DateTime.Now.AddYears(1).AddDays(i),
                                VehicleImageUrl = $"https://example.com/vehicle{i}.jpg",
                                CreatedAt = DateTime.Now.AddMinutes(-i),
                                UpdatedAt = DateTime.Now.AddMinutes(-i)
                            });
                        }
                        await _db.Vehicles.AddRangeAsync(vehicles);
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("20 Vehicles seeded successfully.");

                        // 3. Seed 20 SimCards and assign to vehicles
                        var simCards = new List<SimCard>();
                        for (int i = 1; i <= 20; i++)
                        {
                            simCards.Add(new SimCard
                            {
                                SimNumber = $"SIM{i:0000000000}",
                                Carrier = $"Carrier{i % 3 + 1}",
                                Status = SimCardStatus.Active,
                                CreatedAt = DateTime.Now.AddDays(-i)
                            });
                        }
                        await _db.SimCards.AddRangeAsync(simCards);
                        await _db.SaveChangesAsync();
                        // Assign sim cards to vehicles
                        for (int i = 0; i < 20; i++)
                        {
                            vehicles[i].SimCardId = simCards[i].Id;
                        }
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("20 SimCards seeded and assigned to vehicles successfully.");

                        // 4. Seed 20 Maintenances (each for a vehicle, reported by a user)
                        var maintenances = new List<Maintenance>();
                        for (int i = 0; i < 20; i++)
                        {
                            maintenances.Add(new Maintenance
                            {
                                VehicleId = vehicles[i].Id,
                                ReportedBy = normalUsers[i].Id,
                                IssueDescription = $"Issue {i + 1}",
                                RepairStatus = RepairState.pending,
                                Priority = (PriorityDegree)(i % Enum.GetValues(typeof(PriorityDegree)).Length),
                                CreatedAt = DateTime.Now.AddMinutes(-i)
                            });
                        }
                        await _db.Maintenances.AddRangeAsync(maintenances);
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("20 Maintenances seeded successfully.");

                        // 5. Seed 20 Orders (each by a user)
                        var orders = new List<Order>();
                        for (int i = 0; i < 20; i++)
                        {
                            orders.Add(new Order
                            {
                                UserId = normalUsers[i].Id,
                                VehicleType = (VehicleType)(i % Enum.GetValues(typeof(VehicleType)).Length),
                                PassengerCount = 2 + (i % 4),
                                StartLocation = $"Location-{i + 1}",
                                Destination = $"Destination-{i + 1}",
                                TripStartDate = DateTime.Now.AddHours(i),
                                TripEndDate = DateTime.Now.AddHours(i + 2),
                                Reason = $"Reason {i + 1}",
                                Status = OrderState.Pending,
                                CreatedAt = DateTime.Now.AddMinutes(-i)
                            });
                        }
                        await _db.Orders.AddRangeAsync(orders);
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("20 Orders seeded successfully.");

                        // 6. Seed 20 Trips (each for a vehicle, order, and driver)
                        // Use the seeded driver with Id = "2" for all trips
                        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == "2");
                        var trips = new List<Trip>();
                        for (int i = 0; i < 20; i++)
                        {
                            trips.Add(new Trip
                            {
                                VehicleId = vehicles[i].Id,
                                OrderId = orders[i].Id,
                                DriverId = driver?.Id ?? "2",
                                Distance = 0,
                                Status = TripState.Scheduled,
                                CreatedAt = DateTime.Now.AddMinutes(-i),
                                CreatedBy = normalUsers[i].Id
                            });
                        }
                        await _db.Trips.AddRangeAsync(trips);
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("20 Trips seeded successfully.");

                        // Optionally update driver status
                        if (driver != null)
                        {
                            driver.DriverStatus = DriverState.AssignedOnScheduledTrip;
                            await _db.SaveChangesAsync();
                            _logger.LogInformation("Driver status updated to AssignedOnScheduledTrip due to scheduled trip assignment.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Other data already exists. Skipping vehicle and related data seeding.");
                    }

                    _logger.LogInformation("Commissioner user and other missing data seeded successfully.");
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred while seeding initial data.");
                }
            }
        }
    }
}