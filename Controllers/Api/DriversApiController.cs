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
    public class DriversApiController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DriversApiController(SmartFleetContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DriverDto>>>> GetDrivers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? searchName = null,
            [FromQuery] string? searchLicense = null)
        {
            try
            {
                IQueryable<Driver> driversQuery = _context.Drivers;

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<DriverState>(status, true, out var driverStatus))
                    {
                        driversQuery = driversQuery.Where(d => d.DriverStatus == driverStatus);
                    }
                }

                if (!string.IsNullOrEmpty(searchName))
                {
                    driversQuery = driversQuery.Where(d => d.UserName != null && d.UserName.Contains(searchName));
                }

                if (!string.IsNullOrEmpty(searchLicense))
                {
                    driversQuery = driversQuery.Where(d => d.LicenseNumber.Contains(searchLicense));
                }

                // Get total count for pagination
                var totalCount = await driversQuery.CountAsync();

                // Apply pagination and sorting
                var drivers = await driversQuery
                    .OrderBy(d => d.UserName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var driverDtos = drivers.Select(d => new DriverDto
                {
                    Id = d.Id,
                    UserName = d.UserName ?? "",
                    Email = d.Email ?? "",
                    PhoneNumber = d.PhoneNumber ?? "",
                    LicenseNumber = d.LicenseNumber,
                    LicenseExpiryDate = d.LicenseExpiryDate,
                    DriverStatus = d.DriverStatus.ToString(),
                    ProfileImageUrl = d.ProfileImageUrl ?? "",
                    CreatedAt = d.CreatedAt,
                    AccountStatus = d.AccountStatus
                }).ToList();

                var response = new
                {
                    drivers = driverDtos,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Drivers retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<DriverDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DriverDto>>> GetDriver(string id)
        {
            try
            {
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (driver == null)
                {
                    return NotFound(ApiResponse<DriverDto>.ErrorResponse("Driver not found", 404));
                }

                var driverDto = new DriverDto
                {
                    Id = driver.Id,
                    UserName = driver.UserName ?? "",
                    Email = driver.Email ?? "",
                    PhoneNumber = driver.PhoneNumber ?? "",
                    LicenseNumber = driver.LicenseNumber,
                    LicenseExpiryDate = driver.LicenseExpiryDate,
                    DriverStatus = driver.DriverStatus.ToString(),
                    ProfileImageUrl = driver.ProfileImageUrl ?? "",
                    CreatedAt = driver.CreatedAt,
                    AccountStatus = driver.AccountStatus
                };

                return Ok(ApiResponse<DriverDto>.SuccessResponse(driverDto, "Driver retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DriverDto>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<ApiResponse<List<DriverDto>>>> GetAvailableDrivers()
        {
            try
            {
                var drivers = await _context.Drivers
                    .Where(d => d.DriverStatus == DriverState.Available && d.AccountStatus == true)
                    .OrderBy(d => d.UserName)
                    .ToListAsync();

                var driverDtos = drivers.Select(d => new DriverDto
                {
                    Id = d.Id,
                    UserName = d.UserName ?? "",
                    Email = d.Email ?? "",
                    PhoneNumber = d.PhoneNumber ?? "",
                    LicenseNumber = d.LicenseNumber,
                    LicenseExpiryDate = d.LicenseExpiryDate,
                    DriverStatus = d.DriverStatus.ToString(),
                    ProfileImageUrl = d.ProfileImageUrl ?? "",
                    CreatedAt = d.CreatedAt,
                    AccountStatus = d.AccountStatus
                }).ToList();

                return Ok(ApiResponse<List<DriverDto>>.SuccessResponse(driverDtos, "Available drivers retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<DriverDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }
    }
} 