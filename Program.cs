using SmartFleet.Data;
using SmartFleet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using SmartFleet.Hubs;
using SmartFleet.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SmartFleet.Services.Implemenations;
using SmartFleet.Services.Interfaces;
using SmartFleet.Services.BackgroundServices;

namespace SmartFleet
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddControllers();

            builder.Services.AddDbContext<SmartFleetContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
                option =>
                {
                    //option.Password.RequiredLength = 10; // length for password
                    option.Password.RequireNonAlphanumeric = false;
                    option.Password.RequireUppercase = false;
                })
                .AddEntityFrameworkStores<SmartFleetContext>();

            builder.Services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddCookie(options => {
                        options.LoginPath = "/Account/Login";
                        options.AccessDeniedPath = "/Account/AccessDenied";
                    })
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = builder.Configuration["Jwt:Issuer"],
                            ValidAudience = builder.Configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
                            ClockSkew = TimeSpan.Zero
                        };

                        // Configure JWT for SignalR
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var path = context.HttpContext.Request.Path;
                                if (path.StartsWithSegments("/hubs"))
                                {
                                    // Try query parameter first (for web clients)
                                    var accessToken = context.Request.Query["access_token"];
                                    
                                    // If not found, try Authorization header (for mobile clients)
                                    if (string.IsNullOrEmpty(accessToken))
                                    {
                                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                                        {
                                            accessToken = authHeader.Substring("Bearer ".Length).Trim();
                                        }
                                    }
                                    
                                    if (!string.IsNullOrEmpty(accessToken))
                                    {
                                        context.Token = accessToken;
                                    }
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });

            // Add CORS for mobile app with SignalR support
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MobileAppPolicy", policy =>
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            builder.Services.AddScoped<INotificationService, NotificationService>();
            // Database initializer 
            builder.Services.AddScoped<Dbinitializer>();
            
            // User Role Service
            builder.Services.AddScoped<IUserRoleService, UserRoleService>();
            
            // JWT Service
            builder.Services.AddScoped<IJwtService, JwtService>();

            // Distance Calculation Service
            builder.Services.AddScoped<IDistanceCalculationService, DistanceCalculationService>();

            // Trip State Management Service
            builder.Services.AddScoped<ITripStateManagementService, TripStateManagementService>();

            // Driver Status Management Service
            builder.Services.AddScoped<IDriverStatusManagementService, DriverStatusManagementService>();

            // Vehicle State Management Service
            builder.Services.AddScoped<IVehicleStateManagementService, VehicleStateManagementService>();

            // Background services disabled for API testing
            // builder.Services.AddHostedService<TripStateBackgroundService>();
            // builder.Services.AddHostedService<DriverStatusBackgroundService>();
            // builder.Services.AddHostedService<VehicleStateBackgroundService>();

            builder.Services.AddScoped<IPaginationService, PaginationService>();
            builder.Services.AddScoped<ISearchService, SearchService>();

            // Add SignalR services
            builder.Services.AddSignalR();

            var app = builder.Build();

            // initialize the database
            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<Dbinitializer>();
                await dbInitializer.InitializeAsync();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            
            // Enable CORS
            app.UseCors("MobileAppPolicy");
            
            app.UseAuthentication(); 
            app.UseAuthorization();
            
            app.MapHub<NotificationHub>("/hubs/Notify");
            app.MapHub<TrackingHub>("/hubs/Tracking");

            // Map API Controllers first
            app.MapControllers();
            
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}