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

            return Ok(authResult);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] AuthDTO registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Prevent anonymous creation of Admin accounts
            if (!string.IsNullOrWhiteSpace(registerDto.Role) &&
                registerDto.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var created = await _authBL.RegisterAsync(registerDto);
            if (created == null)
                return BadRequest(new { message = "Registration failed (user may already exist)." });

            return CreatedAtAction(nameof(Login), new { id = created.UserId }, created);
        }
    }
}
