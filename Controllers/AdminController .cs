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
        public async Task<IActionResult> GetResponsesForForm(string formId)
        {
            var responses = await _responseBL.GetResponsesForFormAsync(formId);
            return Ok(responses);
        }
    }
}
