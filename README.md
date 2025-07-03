# SmartFleet

SmartFleet is a digital transformation solution designed to enhance fleet management for organizations such as universities, optimizing vehicle usage, tracking, and administration. It is built with ASP.NET Core 8, Entity Framework Core, and SQL Server, and provides a modern, role-based web interface for managing vehicles, drivers, trips, and more.

## Features

- **Trip Scheduling:** Users can request and schedule trips efficiently, ensuring optimized vehicle allocation.
- **Live Tracking:** Fleet managers can monitor vehicle movements in real-time using GPS data and interactive maps.
- **Vehicle Management:** Add, edit, and track vehicles, including maintenance status and geofencing.
- **Driver Management:** Manage driver profiles, statuses, and assignments.
- **Order Management:** Handle trip orders and assignments with approval workflows.
- **Maintenance Management:** Track and manage vehicle maintenance schedules and statuses.
- **Geofencing:** Define and assign geofences to vehicles for location-based alerts.
- **Notifications:** Real-time notifications for important events (e.g., maintenance, trip status) via SignalR.
- **Role-Based Access:** Supports multiple roles (SysSupport, FleetManager, MaintenanceManager, Commissioner, Driver, NormalUser) with tailored dashboards and permissions.
- **Reports & Analytics:** View statistics and reports on fleet usage, driver activity, and trip history.
- **Database Seeding:** Automatic creation of default roles, users, and sample data on first run.

## Technologies Used

- **ASP.NET Core 8** (MVC & Web API)
- **Entity Framework Core 8** (with SQL Server)
- **SignalR** (for real-time notifications and tracking)
- **Identity** (for authentication and role management)
- **Leaflet.js** (for interactive maps)
- **Bootstrap 5** (for responsive UI)
- **JavaScript & jQuery** (for client-side interactivity)

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (local or remote)

### Setup Instructions

1. **Clone the repository:**
   ```bash
   git clone <repo-url>
   cd SmartFleet
   ```

2. **Configure the database connection:**
   - Edit `appsettings.json` and update the `DefaultConnection` string under `ConnectionStrings` to point to your SQL Server instance.

3. **Apply database migrations and seed data:**
   - On first run, the application will automatically create the database schema and seed roles, users, and sample data.

4. **Run the application:**
   ```bash
   dotnet run
   ```
   - The app will be available at `https://localhost:5001` or `http://localhost:5000` by default.

5. **Login with seeded users:**
   - Example accounts (see `Dbinitializer/Dbinitializer.cs`):
     - SysSupport: `SmartFleet@Support.com` / `123456789k`
     - FleetManager: `fleetmanager@smartfleet.com` / `Password123!`
     - Driver: `driver@smartfleet.com` / `Password123!`
     - Commissioner: `commissioner@smartfleet.com` / `Password123!`
     - MaintenanceManager: `maintenance@smartfleet.com` / `Password123!`
     - NormalUser: `normaluser1@smartfleet.com` / `Password123!`

## Project Structure

- `Controllers/` - MVC and API controllers for all modules
- `Models/` - Entity and view models
- `Data/` - EF Core DbContext and migrations
- `Services/` - Business logic, background services, and interfaces
- `Views/` - Razor views for the web UI
- `wwwroot/` - Static assets (CSS, JS, images)
- `Dbinitializer/` - Database seeding logic

## Customization
- **Roles & Permissions:** Roles are predefined and seeded. You can manage user-role assignments via the UI.
- **Geofencing & Tracking:** Integrates with GPS devices and supports real-time updates via SignalR and Leaflet.js.
- **Notifications:** Uses SignalR for push notifications to the web UI.

## License

This project is for educational and demonstration purposes. Please review and adapt for production use as needed.

---

For questions or contributions, please open an issue or submit a pull request. 