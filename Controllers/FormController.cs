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
    // Allow only authenticated users (Admin and Learner) to view forms.
    [Authorize(Roles = "Admin,Learner")]
    public class FormController : ControllerBase
    {
        private readonly IFormBL _formBL;
 
        public FormController(IFormBL formBL)
        {
            _formBL = formBL ?? throw new ArgumentNullException(nameof(formBL));
        }
 
        /// <summary>
        /// Get all forms accessible to the user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var forms = await _formBL.GetAllFormsAsync();
            return Ok(forms);
        }
 
        /// <summary>
        /// Get a specific form by id.
        /// </summary>
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetById(string id)
        {
            var form = await _formBL.GetFormByIdAsync(id);
            if (form == null)
                return NotFound();
            return Ok(form);
        }
 
        // (Optional) If you want non-admins to be able to search/filter, add endpoints here (e.g., GetActiveForms).
    }
}
