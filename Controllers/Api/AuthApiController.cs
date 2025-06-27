using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Models;
using SmartFleet.Models.DTOs;
using SmartFleet.Services;

namespace SmartFleet.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid request data", 400));
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid email or password", 401));
                }

                // Check if account is active
                if (!user.AccountStatus)
                {
                    return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse("Account is disabled", 401));
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid email or password", 401));
                }

                var roles = await _userManager.GetRolesAsync(user);
                var token = await _jwtService.GenerateTokenAsync(user, roles);
                
                var expireHours = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpireHours"]!);

                var response = new LoginResponseDto
                {
                    Token = token,
                    UserId = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    Roles = roles.ToList(),
                    ProfileImageUrl = user.ProfileImageUrl ?? "",
                    ExpiresAt = DateTime.UtcNow.AddHours(expireHours)
                };

                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Login successful"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<object>>> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                return Ok(ApiResponse<object>.SuccessResponse(null, "Logout successful"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }

        [HttpGet("validate-token")]
        public ActionResult<ApiResponse<object>> ValidateToken()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Missing or invalid token", 401));
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var principal = _jwtService.ValidateToken(token);

                if (principal == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token", 401));
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "Token is valid"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}", 500));
            }
        }
    }
} 