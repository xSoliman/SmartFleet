using SmartFleet.Data;
using SmartFleet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using SmartFleet.Hubs;
using SmartFleet.Services;

namespace SmartFleet
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

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

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options => {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
                                     });

            builder.Services.AddScoped<INotificationService, NotificationService>();
            // Database initializer 
            builder.Services.AddScoped<Dbinitializer>();
            
            // User Role Service
            builder.Services.AddScoped<IUserRoleService, UserRoleService>();

            // Distance Calculation Service
            builder.Services.AddScoped<IDistanceCalculationService, DistanceCalculationService>();

            // Trip State Management Service
            builder.Services.AddScoped<ITripStateManagementService, TripStateManagementService>();

            // Background service for automatic trip state updates
            builder.Services.AddHostedService<TripStateBackgroundService>();

            var app = builder.Build();
            builder.Services.AddSignalR();
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
            app.MapHub<NotificationHub>("/hubs/Notify");
            app.UseAuthentication(); 
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}