using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Newtonsoft.Json.Linq;

namespace FormBuilderAPITests.ControllerTest
{
    public class ResponseControllerTest
    {
        private readonly Mock<IResponseBL> _mockResponseBL;
        private readonly ResponseController _controller;

        public ResponseControllerTest()
        {
            _mockResponseBL = new Mock<IResponseBL>();
            _controller = new ResponseController(_mockResponseBL.Object);
        }

        #region SubmitResponse Tests

        [Fact]
        public async Task SubmitResponse_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            const string userId = "user123";
            var responseDto = new ResponseDTO
            {
                Answers = new List<ResponseAnswerDTO> 
                { 
                    new ResponseAnswerDTO 
                    { 
                        QuestionId = "q1", 
                        AnswerText = "answer1" 
                    } 
                }
            };

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.SubmitResponseAsync(It.IsAny<ResponseDTO>()))
                .ReturnsAsync(1); // Assuming it returns the ResponseId as int

            // Act
            var result = await _controller.SubmitResponse(formId, responseDto);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal($"/api/forms/{formId}/responses", createdResult.Location);
            
            // Instead of trying to access a property directly, convert to dictionary and check
            var resultDict = JObject.FromObject(createdResult.Value);
            Assert.True(resultDict.ContainsKey("id"));
            Assert.Equal(1, resultDict["id"].Value<int>());
            
            // Verify that the response DTO was properly populated
            _mockResponseBL.Verify(bl => bl.SubmitResponseAsync(It.Is<ResponseDTO>(r => 
                r.FormId == formId && 
                r.SubmittedBy == userId && 
                r.SubmittedAt != default(DateTime))), 
                Times.Once);
        }

        [Fact]
        public async Task SubmitResponse_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            var responseDto = new ResponseDTO();
            
            _controller.ModelState.AddModelError("Answers", "Answers are required");

            // Act
            var result = await _controller.SubmitResponse(formId, responseDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitResponse_NoUserIdentity_ReturnsUnauthorized()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            var responseDto = new ResponseDTO();
            
            // Fix: Initialize the controller context with empty claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.SubmitResponse(formId, responseDto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region GetResponsesForForm Tests

        [Fact]
        public async Task GetResponsesForForm_UserHasResponses_ReturnsOkWithResponses()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            const string userId = "user123";
            var expectedResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO 
                { 
                    ResponseId = 1, 
                    FormId = formId, 
                    SubmittedBy = userId,
                    Answers = new List<ResponseAnswerDTO>()
                }
            };

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetResponsesForUserAsync(formId, userId))
                .ReturnsAsync(expectedResponses);

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponses = Assert.IsAssignableFrom<IEnumerable<ResponseDetailDTO>>(okResult.Value);
            Assert.Equal(expectedResponses.Count, returnedResponses.Count());
            Assert.Equal(expectedResponses[0].ResponseId, returnedResponses.First().ResponseId);
        }

        [Fact]
        public async Task GetResponsesForForm_NoUserIdentity_ReturnsUnauthorized()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            
            // Fix: Initialize the controller context with empty claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Convert to dictionary and check
            var resultDict = JObject.FromObject(unauthorizedResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("User ID not found in token.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task GetResponsesForForm_NoResponses_ReturnsNotFound()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            const string userId = "user123";

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetResponsesForUserAsync(formId, userId))
                .ReturnsAsync(new List<ResponseDetailDTO>());

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            
            // Convert to dictionary and check
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this user.", resultDict["message"].Value<string>());
        }

        #endregion

        #region GetAllResponsesByLearner Tests

        [Fact]
        public async Task GetAllResponsesByLearner_UserHasResponses_ReturnsOkWithAllResponses()
        {
            // Arrange
            const string userId = "user123";
            var expectedResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO 
                { 
                    ResponseId = 1, 
                    FormId = "form1",
                    SubmittedBy = userId,
                    Answers = new List<ResponseAnswerDTO>()
                },
                new ResponseDetailDTO 
                { 
                    ResponseId = 2, 
                    FormId = "form2",
                    SubmittedBy = userId,
                    Answers = new List<ResponseAnswerDTO>()
                }
            };

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(userId))
                .ReturnsAsync(expectedResponses);

            // Act
            var result = await _controller.GetAllResponsesByLearner();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponses = Assert.IsAssignableFrom<IEnumerable<ResponseDetailDTO>>(okResult.Value);
            Assert.Equal(expectedResponses.Count, returnedResponses.Count());
            Assert.Equal(expectedResponses[0].ResponseId, returnedResponses.First().ResponseId);
            Assert.Equal(expectedResponses[1].ResponseId, returnedResponses.ElementAt(1).ResponseId);
        }

        [Fact]
        public async Task GetAllResponsesByLearner_NoUserIdentity_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetAllResponsesByLearner();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            
            var resultDict = JObject.FromObject(unauthorizedResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("User ID not found in token.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task GetAllResponsesByLearner_NoResponses_ReturnsNotFound()
        {
            // Arrange
            const string userId = "user123";

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(userId))
                .ReturnsAsync(new List<ResponseDetailDTO>());

            // Act
            var result = await _controller.GetAllResponsesByLearner();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this user.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task GetAllResponsesByLearner_NullResponses_ReturnsNotFound()
        {
            // Arrange
            const string userId = "user123";

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(userId))
                .ReturnsAsync((List<ResponseDetailDTO>)null);

            // Act
            var result = await _controller.GetAllResponsesByLearner();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this user.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task GetAllResponsesByLearner_MultipleFormsResponses_ReturnsAllResponses()
        {
            // Arrange
            const string userId = "user123";
            var expectedResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO 
                { 
                    ResponseId = 1, 
                    FormId = "form1",
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    Answers = new List<ResponseAnswerDTO>
                    {
                        new ResponseAnswerDTO { QuestionId = "q1", AnswerText = "answer1" }
                    }
                },
                new ResponseDetailDTO 
                { 
                    ResponseId = 2, 
                    FormId = "form2",
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Answers = new List<ResponseAnswerDTO>
                    {
                        new ResponseAnswerDTO { QuestionId = "q2", AnswerText = "answer2" }
                    }
                },
                new ResponseDetailDTO 
                { 
                    ResponseId = 3, 
                    FormId = "form1",
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<ResponseAnswerDTO>
                    {
                        new ResponseAnswerDTO { QuestionId = "q1", AnswerText = "updated answer" }
                    }
                }
            };

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(userId))
                .ReturnsAsync(expectedResponses);

            // Act
            var result = await _controller.GetAllResponsesByLearner();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponses = Assert.IsAssignableFrom<IEnumerable<ResponseDetailDTO>>(okResult.Value);
            Assert.Equal(3, returnedResponses.Count());
            
            // Verify all response IDs are present
            var responseIds = returnedResponses.Select(r => r.ResponseId).ToList();
            Assert.Contains(1, responseIds);
            Assert.Contains(2, responseIds);
            Assert.Contains(3, responseIds);
            
            // Verify all responses belong to the same user
            Assert.All(returnedResponses, r => Assert.Equal(userId, r.SubmittedBy));
        }

        #endregion

        #region DownloadFile Tests

        [Fact]
        public async Task DownloadFile_FileExists_ReturnsFileContentResult()
        {
            // Arrange
            const int responseId = 1;
            const int fileId = 2;
            var fileDto = new ResponseFileDTO
            {
                FileName = "test.pdf",
                FileType = "application/pdf",
                Base64Content = "SGVsbG8gV29ybGQ=" // Base64 for "Hello World"
            };

            _mockResponseBL.Setup(bl => bl.GetFileByResponseIdAndFileIdAsync(responseId, fileId))
                .ReturnsAsync(fileDto);

            // Act
            var result = await _controller.DownloadFile(responseId, fileId);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(fileDto.FileType, fileResult.ContentType);
            Assert.Equal(fileDto.FileName, fileResult.FileDownloadName);
            Assert.Equal(Convert.FromBase64String(fileDto.Base64Content), fileResult.FileContents);
        }

        [Fact]
        public async Task DownloadFile_FileNotFound_ReturnsNotFound()
        {
            // Arrange
            const int responseId = 1;
            const int fileId = 2;

            _mockResponseBL.Setup(bl => bl.GetFileByResponseIdAndFileIdAsync(responseId, fileId))
                .ReturnsAsync((ResponseFileDTO)null);

            // Act
            var result = await _controller.DownloadFile(responseId, fileId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            
            // Convert to dictionary and check
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("File not found for this response.", resultDict["message"].Value<string>());
        }

        #endregion

        #region Helper Methods

        private void SetupUserIdentity(ControllerBase controller, string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #endregion
    }
}
