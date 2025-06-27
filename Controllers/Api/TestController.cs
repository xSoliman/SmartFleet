using Microsoft.AspNetCore.Mvc;

namespace SmartFleet.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "API يعمل بنجاح!", timestamp = DateTime.Now });
        }

        [HttpGet("hello")]
        public IActionResult Hello()
        {
            return Ok(new { message = "مرحباً من API!" });
        }
    }
} 