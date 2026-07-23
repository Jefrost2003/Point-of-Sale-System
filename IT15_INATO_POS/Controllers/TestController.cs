using Microsoft.AspNetCore.Mvc;

namespace IT15_INATO_POS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult Get()
        {
            return Ok(new { message = "API is working!", timestamp = DateTime.UtcNow });
        }
    }
}
