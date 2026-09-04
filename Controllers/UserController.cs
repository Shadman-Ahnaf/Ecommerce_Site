using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        // GET: api/user/me
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            string? name =
                User.FindFirstValue(ClaimTypes.Name);

            string? email =
                User.FindFirstValue(ClaimTypes.Email);

            string? role =
                User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                id = userId,
                name = name,
                email = email,
                role = role,
                message = "JWT authentication is working."
            });
        }

        // GET: api/user/buyer-only
        [HttpGet("buyer-only")]
        [Authorize(Roles = "Buyer")]
        public IActionResult BuyerOnly()
        {
            return Ok(new
            {
                message = "You are authorized as a Buyer.",
                user = User.Identity?.Name
            });
        }
    }
}