using SmartFleet.Data;
using SmartFleet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SmartFleet
{
    public class Program
    {
        public static void Main(string[] args)
        {

        

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<SmartFleetContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });


            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(// here change setting or validation for att in userManager
          option =>
          {
              //option.Password.RequiredLength=10 // length for password
              option.Password.RequireNonAlphanumeric = false;
            
          }
          ).
          AddEntityFrameworkStores<SmartFleetContext>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
