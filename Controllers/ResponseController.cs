using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.BusinessLogicLayer;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/responses")]
    public class ResponseController : ControllerBase
    {
        private readonly IResponseBL _responseBL;

        public ResponseController(IResponseBL responseBL)
        {
            _responseBL = responseBL ?? throw new ArgumentNullException(nameof(responseBL));
        }



        [HttpPost("/api/forms/{formId:length(24)}/responses")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> SubmitResponse(string formId, [FromBody] ResponseDTO responseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            responseDto.FormId = formId;
            responseDto.SubmittedBy = userId;
            responseDto.SubmittedAt = DateTime.UtcNow;

            var createdResponseId = await _responseBL.SubmitResponseAsync(responseDto);

            return Created($"/api/forms/{formId}/responses", new { id = createdResponseId });
        }


        [HttpGet("/api/forms/{formId:length(24)}/responses")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetResponsesForForm(string formId)
        {
            // 1️⃣ Get the logged-in user's ID from JWT token claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "User ID not found in token." });

            // 2️⃣ Fetch only this user's responses for the form
            var responses = await _responseBL.GetResponsesForUserAsync(formId, userId);

            // 3️⃣ If the user hasn’t submitted any response, show empty or 404
            if (responses == null || !responses.Any())
                return NotFound(new { message = "No responses found for this user." });

            // 4️⃣ Return OK with their responses
            return Ok(responses);
        }


    }
}
