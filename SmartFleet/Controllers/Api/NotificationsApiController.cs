using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Models.DTOs;
using SmartFleet.Services;

namespace SmartFleet.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class NotificationsApiController : ControllerBase
    {
        private readonly SmartFleetContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public NotificationsApiController(
            SmartFleetContext context, 
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isRead = null)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<List<NotificationDto>>.ErrorResponse("User not found", 401));
                }

                IQueryable<Notification> notificationsQuery = _context.Notifications
                    .Where(n => n.UserId == currentUser.Id);

                // Apply read status filter
                if (isRead.HasValue)
                {
                    notificationsQuery = notificationsQuery.Where(n => n.IsRead == isRead.Value);
                }

                // Get total count for pagination
                var totalCount = await notificationsQuery.CountAsync();

                // Apply pagination and sorting (newest first)
                var notifications = await notificationsQuery
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var notificationDtos = notifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    RelatedTable = n.RelatedTable.ToString(),
                    RelatedId = n.RelatedId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

                // Count unread notifications
                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == currentUser.Id && !n.IsRead)
                    .CountAsync();

                var response = new
                {
                    notifications = notificationDtos,
                    unreadCount = unreadCount,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Notifications retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<NotificationDto>>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotification(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<NotificationDto>.ErrorResponse("User not found", 401));
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

                if (notification == null)
                {
                    return NotFound(ApiResponse<NotificationDto>.ErrorResponse("Notification not found", 404));
                }

                var notificationDto = new NotificationDto
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Message = notification.Message,
                    RelatedTable = notification.RelatedTable.ToString(),
                    RelatedId = notification.RelatedId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                return Ok(ApiResponse<NotificationDto>.SuccessResponse(notificationDto, "Notification retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<NotificationDto>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}/mark-read")]
        public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not found", 401));
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

                if (notification == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Notification not found", 404));
                }

                if (!notification.IsRead)
                {
                    await _notificationService.MarkNotificationAsReadAsync(id);
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "Notification marked as read"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpPut("mark-all-read")]
        public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not found", 401));
                }

                await _notificationService.MarkAllNotificationAsReadAsync(currentUser.Id);

                return Ok(ApiResponse<object>.SuccessResponse(null, "All notifications marked as read"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<object>>> GetUnreadCount()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not found", 401));
                }

                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == currentUser.Id && !n.IsRead)
                    .CountAsync();

                var response = new { unreadCount = unreadCount };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Unread count retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpPost("test")]
        public async Task<ActionResult<ApiResponse<object>>> SendTestNotification()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not found", 401));
                }

                // Send broadcast notification for testing
                await _notificationService.CreateBroadcastNotificationAsync(
                    "Test Notification",
                    $"This is a test notification sent at {DateTime.Now:HH:mm:ss} by {currentUser.UserName}",
                    RelatedTable.None
                );

                return Ok(ApiResponse<object>.SuccessResponse(null, "Test notification sent successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }
    }
} 