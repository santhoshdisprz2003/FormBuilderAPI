using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.BusinessLogicLayer;
using System.Security.Claims;
using System.Linq;

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

        // ✅ GET all forms with pagination
        [HttpGet]
        public async Task<IActionResult> GetAllForms(int offset = 0, int limit = 10)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "Learner";

            var (forms, totalCount) = await _formBL.GetAllFormsAsync(userRole, offset, limit);

            var response = new
            {
                TotalCount = totalCount,
                Offset = offset,
                Limit = limit,
                Data = forms
            };

            return Ok(response);
        }

        // ✅ GET form by ID
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFormById(string id)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "Learner";
            var form = await _formBL.GetFormByIdAsync(id, userRole);

            if (form == null)
                return NotFound(new { message = "Form not found or not accessible." });

            return Ok(form);
        }

        // ✅ POST - Create new form (configuration only)
        [HttpPost("formconfig")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFormConfig([FromBody] FormConfigDTO configDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException("CreatedBy cannot be null");

            var newFormId = await _formBL.CreateFormConfigAsync(configDto, userName);
            return CreatedAtAction(nameof(GetFormById), new { id = newFormId }, new { id = newFormId });
        }

        // ✅ PUT - Update form (title, description, layout)
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

        // ✅ DELETE - Delete form
        [HttpDelete("{id:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteForm(string id)
        {
            var deleted = await _formBL.DeleteFormAsync(id);
            if (!deleted)
                return NotFound(new { message = "Form not found." });

            return Ok(new { message = "Form deleted successfully." });
        }

        // ✅ PUT - Publish form
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
