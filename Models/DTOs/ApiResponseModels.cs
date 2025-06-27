using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Models.DTOs
{
    // Base response model
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 200
            };
        }
        
        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default(T),
                StatusCode = statusCode
            };
        }
    }

    // Login DTOs
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        
        [Required]
        public string Password { get; set; } = "";
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = "";
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new List<string>();
        public string ProfileImageUrl { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

    // Trip DTOs
    public class TripDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleLicensePlate { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public string VehicleType { get; set; } = "";
        public int OrderId { get; set; }
        public string DriverId { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string DriverLicenseNumber { get; set; } = "";
        public decimal Distance { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
        public string CreatedByUserName { get; set; } = "";
        
        // Order details
        public string StartLocation { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime TripStartDate { get; set; }
        public DateTime TripEndDate { get; set; }
        public string Reason { get; set; } = "";
        public int PassengerCount { get; set; }
    }

    // Order DTOs
    public class OrderDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string VehicleType { get; set; } = "";
        public int PassengerCount { get; set; }
        public string StartLocation { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime TripStartDate { get; set; }
        public DateTime TripEndDate { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        
        // Trip info if exists
        public int? TripId { get; set; }
        public string? TripStatus { get; set; }
    }

    // Vehicle DTOs
    public class VehicleDto
    {
        public int Id { get; set; }
        public string Model { get; set; } = "";
        public string Type { get; set; } = "";
        public int Capacity { get; set; }
        public string VehicleImageUrl { get; set; } = "";
        public string LicensePlate { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalDistanceTraveled { get; set; }
        public DateTime? RegistrationExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // SimCard info
        public int? SimCardId { get; set; }
        public string? SimCardNumber { get; set; }
        public string? SimCardCarrier { get; set; }
        public string? SimCardStatus { get; set; }
    }

    // Driver DTOs
    public class DriverDto
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public DateTime LicenseExpiryDate { get; set; }
        public string DriverStatus { get; set; } = "";
        public string ProfileImageUrl { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool AccountStatus { get; set; }
    }

    // Notification DTOs
    public class NotificationDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string RelatedTable { get; set; } = "";
        public int? RelatedId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 