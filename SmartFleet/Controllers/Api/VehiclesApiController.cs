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
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "FleetManager,SysSupport")]
    public class VehiclesApiController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VehiclesApiController(SmartFleetContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<VehicleDto>>>> GetVehicles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? vehicleType = null,
            [FromQuery] string? searchModel = null,
            [FromQuery] string? searchPlate = null)
        {
            try
            {
                IQueryable<Vehicle> vehiclesQuery = _context.Vehicles
                    .Include(v => v.SimCard);

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<VehicleState>(status, true, out var vehicleStatus))
                    {
                        vehiclesQuery = vehiclesQuery.Where(v => v.Status == vehicleStatus);
                    }
                }

                if (!string.IsNullOrEmpty(vehicleType))
                {
                    if (Enum.TryParse<VehicleType>(vehicleType, true, out var vType))
                    {
                        vehiclesQuery = vehiclesQuery.Where(v => v.Type == vType);
                    }
                }

                if (!string.IsNullOrEmpty(searchModel))
                {
                    vehiclesQuery = vehiclesQuery.Where(v => v.Model.Contains(searchModel));
                }

                if (!string.IsNullOrEmpty(searchPlate))
                {
                    vehiclesQuery = vehiclesQuery.Where(v => v.LicensePlate.Contains(searchPlate));
                }

                // Get total count for pagination
                var totalCount = await vehiclesQuery.CountAsync();

                // Apply pagination and sorting
                var vehicles = await vehiclesQuery
                    .OrderBy(v => v.LicensePlate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var vehicleDtos = vehicles.Select(v => new VehicleDto
                {
                    Id = v.Id,
                    Model = v.Model,
                    Type = v.Type.ToString(),
                    Capacity = v.Capacity,
                    VehicleImageUrl = v.VehicleImageUrl ?? "",
                    LicensePlate = v.LicensePlate,
                    Status = v.Status.ToString(),
                    TotalDistanceTraveled = v.TotalDistanceTraveled,
                    RegistrationExpiryDate = v.RegistrationExpiryDate,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt,
                    SimCardId = v.SimCardId,
                    SimCardNumber = v.SimCard?.SimNumber,
                    SimCardCarrier = v.SimCard?.Carrier,
                    SimCardStatus = v.SimCard?.Status.ToString()
                }).ToList();

                var response = new
                {
                    vehicles = vehicleDtos,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Vehicles retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<VehicleDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<VehicleDto>>> GetVehicle(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.SimCard)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle == null)
                {
                    return NotFound(ApiResponse<VehicleDto>.ErrorResponse("Vehicle not found", 404));
                }

                var vehicleDto = new VehicleDto
                {
                    Id = vehicle.Id,
                    Model = vehicle.Model,
                    Type = vehicle.Type.ToString(),
                    Capacity = vehicle.Capacity,
                    VehicleImageUrl = vehicle.VehicleImageUrl ?? "",
                    LicensePlate = vehicle.LicensePlate,
                    Status = vehicle.Status.ToString(),
                    TotalDistanceTraveled = vehicle.TotalDistanceTraveled,
                    RegistrationExpiryDate = vehicle.RegistrationExpiryDate,
                    CreatedAt = vehicle.CreatedAt,
                    UpdatedAt = vehicle.UpdatedAt,
                    SimCardId = vehicle.SimCardId,
                    SimCardNumber = vehicle.SimCard?.SimNumber,
                    SimCardCarrier = vehicle.SimCard?.Carrier,
                    SimCardStatus = vehicle.SimCard?.Status.ToString()
                };

                return Ok(ApiResponse<VehicleDto>.SuccessResponse(vehicleDto, "Vehicle retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<VehicleDto>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<ApiResponse<List<VehicleDto>>>> GetAvailableVehicles()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Include(v => v.SimCard)
                    .Where(v => v.Status == VehicleState.available || v.Status == VehicleState.maintained)
                    .OrderBy(v => v.LicensePlate)
                    .ToListAsync();

                var vehicleDtos = vehicles.Select(v => new VehicleDto
                {
                    Id = v.Id,
                    Model = v.Model,
                    Type = v.Type.ToString(),
                    Capacity = v.Capacity,
                    VehicleImageUrl = v.VehicleImageUrl ?? "",
                    LicensePlate = v.LicensePlate,
                    Status = v.Status.ToString(),
                    TotalDistanceTraveled = v.TotalDistanceTraveled,
                    RegistrationExpiryDate = v.RegistrationExpiryDate,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt,
                    SimCardId = v.SimCardId,
                    SimCardNumber = v.SimCard?.SimNumber,
                    SimCardCarrier = v.SimCard?.Carrier,
                    SimCardStatus = v.SimCard?.Status.ToString()
                }).ToList();

                return Ok(ApiResponse<List<VehicleDto>>.SuccessResponse(vehicleDtos, "Available vehicles retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<VehicleDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }
    }
} 