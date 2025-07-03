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
    public class OrdersApiController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersApiController(SmartFleetContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders(
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
                    return Unauthorized(ApiResponse<List<OrderDto>>.ErrorResponse("User not found", 401));
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isFleetManager = userRoles.Contains("FleetManager");
                var isSystemSupport = userRoles.Contains("SysSupport");
                var isNormalUser = userRoles.Contains("NormalUser");

                IQueryable<Order> ordersQuery = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Trip);

                // Apply role-based filtering
                if (isNormalUser)
                {
                    // Normal users see only their own orders
                    ordersQuery = ordersQuery.Where(o => o.UserId == currentUser.Id);
                }
                else if (isFleetManager || isSystemSupport)
                {
                    // Fleet Managers and System Support see all orders
                    // No additional filtering needed
                }
                else
                {
                    // Other roles see only their own orders
                    ordersQuery = ordersQuery.Where(o => o.UserId == currentUser.Id);
                }

                // Apply additional filters
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<OrderState>(status, true, out var orderStatus))
                    {
                        ordersQuery = ordersQuery.Where(o => o.Status == orderStatus);
                    }
                }

                if (!string.IsNullOrEmpty(destination))
                {
                    ordersQuery = ordersQuery.Where(o => o.Destination.Contains(destination));
                }

                // Get total count for pagination
                var totalCount = await ordersQuery.CountAsync();

                // Apply pagination and sorting
                var orders = await ordersQuery
                    .OrderBy(o => o.Status == OrderState.Pending ? 0 :
                               o.Status == OrderState.Approved ? 1 :
                               o.Status == OrderState.Rejected ? 2 : 3)
                    .ThenByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var orderDtos = orders.Select(o => new OrderDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    UserName = o.User.UserName ?? "",
                    UserEmail = o.User.Email ?? "",
                    VehicleType = o.VehicleType.ToString(),
                    PassengerCount = o.PassengerCount,
                    StartLocation = o.StartLocation,
                    Destination = o.Destination,
                    TripStartDate = o.TripStartDate,
                    TripEndDate = o.TripEndDate,
                    Reason = o.Reason,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    TripId = o.Trip?.Id,
                    TripStatus = o.Trip?.Status.ToString()
                }).ToList();

                var response = new
                {
                    orders = orderDtos,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Orders retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<OrderDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("User not found", 401));
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isFleetManager = userRoles.Contains("FleetManager");
                var isSystemSupport = userRoles.Contains("SysSupport");
                var isNormalUser = userRoles.Contains("NormalUser");

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Trip)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found", 404));
                }

                // Check permissions
                if (isNormalUser && order.UserId != currentUser.Id)
                {
                    return Forbid();
                }

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    UserName = order.User.UserName ?? "",
                    UserEmail = order.User.Email ?? "",
                    VehicleType = order.VehicleType.ToString(),
                    PassengerCount = order.PassengerCount,
                    StartLocation = order.StartLocation,
                    Destination = order.Destination,
                    TripStartDate = order.TripStartDate,
                    TripEndDate = order.TripEndDate,
                    Reason = order.Reason,
                    Status = order.Status.ToString(),
                    CreatedAt = order.CreatedAt,
                    TripId = order.Trip?.Id,
                    TripStatus = order.Trip?.Status.ToString()
                };

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto, "Order retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }
    }
} 