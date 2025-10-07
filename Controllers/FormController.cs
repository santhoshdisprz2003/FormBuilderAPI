using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.BusinessLogicLayer;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/forms")]
    [Authorize(Roles = "Admin,Learner")]
    public class FormController : ControllerBase
    {
        private readonly IFormBL _formBL;

        public FormController(IFormBL formBL)
        {
            _formBL = formBL ?? throw new ArgumentNullException(nameof(formBL));
        }

        /// <summary>
        /// Get all forms (accessible to both Admin and Learner).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllForms()
        {
            var forms = await _formBL.GetAllFormsAsync();
            return Ok(forms);
        }

        /// <summary>
        /// Get a specific form by ID (accessible to both Admin and Learner).
        /// </summary>
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFormById(string id)
        {
            var form = await _formBL.GetFormByIdAsync(id);
            if (form == null)
                return NotFound(new { message = "Form not found." });

            return Ok(form);
        }

        /// <summary>
        /// Create a new form (Admin only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateForm([FromBody] FormDTO formDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newFormId = await _formBL.CreateFormAsync(formDto);
            return CreatedAtAction(nameof(GetFormById), new { id = newFormId }, new { id = newFormId });
        }

        /// <summary>
        /// Update an existing form (Admin only).
        /// </summary>
        [HttpPut("{id:length(24)}")]
        [Authorize(Roles = "Admin")]
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
        /// Delete a form by ID (Admin only).
        /// </summary>
        [HttpDelete("{id:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteForm(string id)
        {
            var deleted = await _formBL.DeleteFormAsync(id);
            if (!deleted)
                return NotFound(new { message = "Form not found." });

            return NoContent();
        }
    }
}
