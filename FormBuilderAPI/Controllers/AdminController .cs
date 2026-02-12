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
        private readonly IResponseBL _responseBL;

        public AdminController(IFormBL formBL, IResponseBL responseBL)
        {
            _formBL = formBL ?? throw new ArgumentNullException(nameof(formBL));
            _responseBL = responseBL ?? throw new ArgumentNullException(nameof(responseBL));
        }

       
        [HttpGet("forms/{formId:length(24)}/responses")]
[Authorize(Roles = "Admin")] // Optional: add this if it’s an admin-only view
public async Task<IActionResult> GetResponsesForForm(
    string formId,
    [FromQuery] string? search,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 6)
{
    // 1️⃣ Fetch all responses for this form (with optional search + pagination)
    var pagedResult = await _responseBL.GetResponsesForFormAsync(formId, search, pageNumber, pageSize);

    // 2️⃣ If no responses found
    if (pagedResult == null)
        return NotFound(new { message = "No responses found for this form." });

    // 3️⃣ Return paginated + filtered responses
    return Ok(pagedResult);
}

    }
}
