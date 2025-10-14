using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.BusinessLogicLayer;
using System.Security.Claims;

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

        [HttpGet]
        public async Task<IActionResult> GetAllForms()
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "Learner";
            var forms = await _formBL.GetAllFormsAsync(userRole);
            return Ok(forms);
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFormById(string id)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "Learner";
            var form = await _formBL.GetFormByIdAsync(id, userRole);

            if (form == null)
                return NotFound(new { message = "Form not found or not accessible." });

            return Ok(form);
        }

        [HttpPost("Formconfig")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFormConfig([FromBody] FormConfigDTO configDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                var userName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;


            var newFormId = await _formBL.CreateFormConfigAsync(configDto,userName);
            return CreatedAtAction(nameof(GetFormById), new { id = newFormId }, new { id = newFormId });
        }

        [HttpPost("{formId:length(24)}/Formlayout")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFormLayout(string formId, [FromBody] FormLayoutDTO layoutDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _formBL.CreateFormLayoutAsync(formId, layoutDto);
                if (!success)
                    return NotFound(new { message = "Form not found or layout not updated." });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateForm(string id, [FromBody] FormDTO formDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _formBL.UpdateFormAsync(id, formDto);
            if (!updated)
                return BadRequest(new { message = "Cannot edit a published form or form not found." });

            return Ok(new { message = "Form updated successfully." });
        }

        [HttpDelete("{id:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteForm(string id)
        {
            var deleted = await _formBL.DeleteFormAsync(id);
            if (!deleted)
                return NotFound(new { message = "Form not found." });

            return Ok(new { message = "Form deleted successfully." });
        }

        [HttpPut("{id:length(24)}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PublishForm(string id)
        {
            try
            {
                var publishedForm = await _formBL.PublishFormAsync(id);
                return Ok(publishedForm);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return NotFound(new { message = "Form not found." });
            }
        }
    }
}
