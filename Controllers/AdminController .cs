using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.BusinessLogicLayer;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IFormBL _formBL;

        public AdminController(IFormBL formBL)
        {
            _formBL = formBL ?? throw new ArgumentNullException(nameof(formBL));
        }

        /// <summary>
        /// Get a specific form by ID (Admin only).
        /// </summary>
        [HttpGet("forms/{id:length(24)}")]
        public async Task<IActionResult> GetFormById(string id)
        {
            var form = await _formBL.GetFormByIdAsync(id);
            if (form == null)
                return NotFound(new { message = "Form not found." });

            return Ok(form);
        }
    }
}
