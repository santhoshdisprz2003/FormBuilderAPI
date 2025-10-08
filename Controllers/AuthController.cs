using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.BusinessLogicLayer;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthBL _authBL;

        public AuthController(IAuthBL authBL)
        {
            _authBL = authBL ?? throw new ArgumentNullException(nameof(authBL));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] AuthDTO authDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResult = await _authBL.LoginAsync(authDto);
            if (authResult == null)
                return Unauthorized(new { message = "Invalid username or password." });

            // ✅ Success message for login
            return Ok(new
            {
                message = "Login successful.",
                data = authResult
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] AuthDTO registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Prevent Admin registration via API
            if (!string.IsNullOrWhiteSpace(registerDto.Role) &&
                registerDto.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Admin accounts cannot be registered via API. Please contact the system administrator."
                });
            }

            // 🔍 Check if username already exists
            var existingUser = await _authBL.GetUserByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                return Conflict(new
                {
                    message = "User already exists. Please log in instead."
                });
            }

            // ✅ Register new learner
            var created = await _authBL.RegisterAsync(registerDto);
            if (created == null)
            {
                return BadRequest(new
                {
                    message = "Registration failed. Please try again later."
                });
            }

            // ✅ Success message for registration
            return CreatedAtAction(nameof(Login), new { id = created.UserId }, new
            {
                message = "Registration successful. You can now log in.",
                data = created
            });
        }
    }
}
