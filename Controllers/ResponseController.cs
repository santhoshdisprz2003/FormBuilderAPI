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

        /// <summary>
        /// Submit a form response - Learner only.
        /// POST api/responses/forms/{formId}/responses
        /// </summary>
        [HttpPost("forms/{formId:length(24)}/responses")]
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
            return CreatedAtAction(nameof(GetResponseById), new { id = createdResponseId }, new { id = createdResponseId });
        }

        /// <summary>
        /// Admin: get responses for a given form.
        /// GET api/responses/forms/{formId}/responses
        /// </summary>
        [HttpGet("forms/{formId:length(24)}/responses")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetResponsesForForm(string formId)
        {
            var responses = await _responseBL.GetResponsesForFormAsync(formId);
            return Ok(responses);
        }

        /// <summary>
        /// Admin: get a specific response by id.
        /// GET api/responses/{id}
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetResponseById(Guid id)
        {
            var response = await _responseBL.GetResponseByIdAsync(id);
            if (response == null)
                return NotFound();

            return Ok(response);
        }

        /// <summary>
        /// Admin: delete a response.
        /// DELETE api/responses/{id}
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteResponse(Guid id)
        {
            var deleted = await _responseBL.DeleteResponseAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Admin: get all responses.
        /// GET api/responses
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllResponses()
        {
            var responses = await _responseBL.GetAllResponsesAsync();
            return Ok(responses);
        }
    }
}
