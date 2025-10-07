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
        /// Create a new form layout (Admin-only).
        /// </summary>
        [HttpPost("forms")]
        public async Task<IActionResult> CreateForm([FromBody] FormDTO formDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
 
            var newFormId = await _formBL.CreateFormAsync(formDto);
            // newFormId expected as string (MongoDB ObjectId) — adjust if using GUID.
            return CreatedAtAction(nameof(GetFormById), new { id = newFormId }, new { id = newFormId });
        }
 
        /// <summary>
        /// Get all forms (Admin).
        /// </summary>
        [HttpGet("forms")]
        public async Task<IActionResult> GetAllForms()
        {
            var forms = await _formBL.GetAllFormsAsync();
            return Ok(forms);
        }
 
        /// <summary>
        /// Get a single form by id (Admin).
        /// </summary>
        [HttpGet("forms/{id:length(24)}")]
        public async Task<IActionResult> GetFormById(string id)
        {
            var form = await _formBL.GetFormByIdAsync(id);
            if (form == null)
                return NotFound();
            return Ok(form);
        }
 
        /// <summary>
        /// Update an existing form (Admin).
        /// </summary>
        [HttpPut("forms/{id:length(24)}")]
        public async Task<IActionResult> UpdateForm(string id, [FromBody] FormDTO formDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
 
            var updated = await _formBL.UpdateFormAsync(id, formDto);
            if (!updated)
                return NotFound(new { message = "Form not found." });
 
            return NoContent();
        }
 
        /// <summary>
        /// Delete a form by id (Admin).
        /// </summary>
        [HttpDelete("forms/{id:length(24)}")]
        public async Task<IActionResult> DeleteForm(string id)
        {
            var deleted = await _formBL.DeleteFormAsync(id);
            if (!deleted)
                return NotFound(new { message = "Form not found." });
 
            return NoContent();
        }
    }
}
