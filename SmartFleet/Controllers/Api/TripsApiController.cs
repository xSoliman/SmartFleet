using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Models.DTOs;

namespace SmartFleet.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class TripsApiController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TripsApiController(SmartFleetContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<TripDto>>>> GetTrips(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? destination = null)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<List<TripDto>>.ErrorResponse("User not found", 401));
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isDriver = userRoles.Contains("Driver");
                var isFleetManager = userRoles.Contains("FleetManager");
                var isSystemSupport = userRoles.Contains("SysSupport");
                var isNormalUser = userRoles.Contains("NormalUser");

                IQueryable<Trip> tripsQuery = _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .Include(t => t.Order)
                    .ThenInclude(o => o.User)
                    .Include(t => t.CreatedByUser);

                // Apply role-based filtering
                if (isDriver)
                {
                    // Drivers see only trips assigned to them
                    tripsQuery = tripsQuery.Where(t => t.DriverId == currentUser.Id);
                }
                else if (isNormalUser)
                {
                    // Normal users see trips from their own orders
                    tripsQuery = tripsQuery.Where(t => t.Order.UserId == currentUser.Id);
                }
                else if (isFleetManager || isSystemSupport)
                {
                    // Fleet Managers and System Support see all trips
                    // No additional filtering needed
                }
                else
                {
                    // Other roles see trips from orders they created
                    tripsQuery = tripsQuery.Where(t => t.Order.UserId == currentUser.Id);
                }

                // Apply additional filters
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<TripState>(status, true, out var tripStatus))
                    {
                        tripsQuery = tripsQuery.Where(t => t.Status == tripStatus);
                    }
                }

                if (!string.IsNullOrEmpty(destination))
                {
                    tripsQuery = tripsQuery.Where(t => t.Order.Destination.Contains(destination));
                }

                // Get total count for pagination
                var totalCount = await tripsQuery.CountAsync();

                // Apply pagination and sorting
                var trips = await tripsQuery
                    .OrderBy(t => t.Status == TripState.InProgress ? 0 :
                               t.Status == TripState.Scheduled ? 1 :
                               t.Status == TripState.Completed ? 2 : 3)
                    .ThenBy(t => t.Order.TripStartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var tripDtos = trips.Select(t => new TripDto
                {
                    Id = t.Id,
                    VehicleId = t.VehicleId,
                    VehicleLicensePlate = t.Vehicle.LicensePlate,
                    VehicleModel = t.Vehicle.Model,
                    VehicleType = t.Vehicle.Type.ToString(),
                    OrderId = t.OrderId,
                    DriverId = t.DriverId,
                    DriverName = t.Driver.UserName ?? "",
                    DriverLicenseNumber = t.Driver.LicenseNumber,
                    Distance = t.Distance,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt,
                    CreatedBy = t.CreatedBy,
                    CreatedByUserName = t.CreatedByUser.UserName ?? "",
                    StartLocation = t.Order.StartLocation,
                    Destination = t.Order.Destination,
                    TripStartDate = t.Order.TripStartDate,
                    TripEndDate = t.Order.TripEndDate,
                    Reason = t.Order.Reason,
                    PassengerCount = t.Order.PassengerCount
                }).ToList();

                var response = new
                {
                    trips = tripDtos,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Trips retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<TripDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TripDto>>> GetTrip(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<TripDto>.ErrorResponse("User not found", 401));
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isDriver = userRoles.Contains("Driver");
                var isNormalUser = userRoles.Contains("NormalUser");
                var isFleetManager = userRoles.Contains("FleetManager");
                var isSystemSupport = userRoles.Contains("SysSupport");

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .Include(t => t.Order)
                    .ThenInclude(o => o.User)
                    .Include(t => t.CreatedByUser)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (trip == null)
                {
                    return NotFound(ApiResponse<TripDto>.ErrorResponse("Trip not found", 404));
                }

                // Check permissions
                if (isDriver && trip.DriverId != currentUser.Id)
                {
                    return Forbid();
                }
                else if (isNormalUser && trip.Order.UserId != currentUser.Id)
                {
                    return Forbid();
                }

                var tripDto = new TripDto
                {
                    Id = trip.Id,
                    VehicleId = trip.VehicleId,
                    VehicleLicensePlate = trip.Vehicle.LicensePlate,
                    VehicleModel = trip.Vehicle.Model,
                    VehicleType = trip.Vehicle.Type.ToString(),
                    OrderId = trip.OrderId,
                    DriverId = trip.DriverId,
                    DriverName = trip.Driver.UserName ?? "",
                    DriverLicenseNumber = trip.Driver.LicenseNumber,
                    Distance = trip.Distance,
                    Status = trip.Status.ToString(),
                    CreatedAt = trip.CreatedAt,
                    CreatedBy = trip.CreatedBy,
                    CreatedByUserName = trip.CreatedByUser.UserName ?? "",
                    StartLocation = trip.Order.StartLocation,
                    Destination = trip.Order.Destination,
                    TripStartDate = trip.Order.TripStartDate,
                    TripEndDate = trip.Order.TripEndDate,
                    Reason = trip.Order.Reason,
                    PassengerCount = trip.Order.PassengerCount
                };

                return Ok(ApiResponse<TripDto>.SuccessResponse(tripDto, "Trip retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<TripDto>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }
    }
} 