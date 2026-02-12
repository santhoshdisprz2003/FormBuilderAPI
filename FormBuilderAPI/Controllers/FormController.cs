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
        public async Task<IActionResult> GetAllForms(int pageNumber = 1, int pageSize = 9, string? search = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 9;

            var userRole = User.IsInRole("Admin") ? "Admin" : "Learner";

            var (forms, totalCount) = await _formBL.GetAllFormsAsync(userRole, pageNumber, pageSize, search);

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var response = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Search = search,
                Data = forms
            };

            return Ok(response);
        }


        // ✅ GET form by ID
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetFormById(string id)
        {
            //  Console.WriteLine($"🔍 Fetching form for ID: {id}");
            var userRole = User.IsInRole("Admin") ? "Admin" : "Learner";
            // Console.WriteLine($"👤 Role: {userRole}");

            var form = await _formBL.GetFormByIdAsync(id, userRole);
            if (form == null)
            {
                Console.WriteLine("❌ Form not found or not accessible.");
                return NotFound(new { message = "Form not found or not accessible." });
            }

            // Console.WriteLine($"✅ Form fetched successfully: {System.Text.Json.JsonSerializer.Serialize(form)}");
            return Ok(form);

        }

        // ✅ POST - Create Form Config
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


        // ✅ PUT - Update Form Config
        [HttpPut("formconfig/{id:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFormConfig(string id, [FromBody] FormConfigDTO configDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _formBL.UpdateFormConfigAsync(id, configDto);
                if (!updated)
                    return NotFound(new { message = "Form not found." });

                return Ok(new { message = "Form configuration updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ POST - Create Form Layout
        [HttpPost("formlayout/{formId:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFormLayout(string formId, [FromBody] FormLayoutDTO layoutDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _formBL.CreateFormLayoutAsync(formId, layoutDto);
                if (!created)
                    return BadRequest(new { message = "Form layout creation failed." });

                return Ok(new { message = "Form layout created successfully." });
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



        // ✅ PUT - Update Form Layout
        [HttpPut("formlayout/{formId:length(24)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFormLayout(string formId, [FromBody] FormLayoutDTO layoutDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _formBL.UpdateFormLayoutAsync(formId, layoutDto);
                if (!updated)
                    return NotFound(new { message = "Form not found." });

                return Ok(new { message = "Form layout updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
    }
}
