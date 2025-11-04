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

        #region Constructor Tests

        [Fact]
        public void Constructor_NullResponseBL_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ResponseController(null!));
        }

        #endregion

        #region SubmitResponse Tests

        [Fact]
        public async Task SubmitResponse_ValidRequest_ReturnsCreated()
        {
            // Arrange
            const string formId = "507f1f77bcf86cd799439011";
            const string userId = "user123";
            var responseDto = new ResponseDTO
            {
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q1",
                        AnswerText = "Answer 1"
                    }
                }
            };

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.SubmitResponseAsync(It.IsAny<ResponseDTO>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.SubmitResponse(formId, responseDto);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal($"/api/forms/{formId}/responses", createdResult.Location);

            var resultDict = JObject.FromObject(createdResult.Value!);
            Assert.True(resultDict.ContainsKey("id"));
            Assert.Equal(1, resultDict["id"]!.Value<int>());

            // Verify the DTO was populated correctly
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
            const string formId = "507f1f77bcf86cd799439011";
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
            const string formId = "507f1f77bcf86cd799439011";
            var responseDto = new ResponseDTO
            {
                Answers = new List<ResponseAnswerDTO>()
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.SubmitResponse(formId, responseDto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SubmitResponse_SetsFormIdFromRoute()
        {
            // Arrange
            const string formId = "507f1f77bcf86cd799439011";
            const string userId = "user123";
            var responseDto = new ResponseDTO
            {
                FormId = "different-form-id", // Should be overridden
                Answers = new List<ResponseAnswerDTO>()
            };

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.SubmitResponseAsync(It.IsAny<ResponseDTO>()))
                .ReturnsAsync(1);

            // Act
            await _controller.SubmitResponse(formId, responseDto);

            // Assert
            _mockResponseBL.Verify(bl => bl.SubmitResponseAsync(It.Is<ResponseDTO>(r =>
                r.FormId == formId)), // Should use route parameter
                Times.Once);
        }

        #endregion

        #region GetResponsesForForm Tests

        [Fact]
        public async Task GetResponsesForForm_UserHasResponses_ReturnsOkWithResponses()
        {
            // Arrange
            const string formId = "507f1f77bcf86cd799439011";
            const string userId = "user123";
            var expectedResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 1,
                    FormId = formId,
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow,
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
            const string formId = "507f1f77bcf86cd799439011";

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);

            var resultDict = JObject.FromObject(unauthorizedResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("User ID not found in token.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetResponsesForForm_NoResponses_ReturnsNotFound()
        {
            // Arrange
            const string formId = "507f1f77bcf86cd799439011";
            const string userId = "user123";

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetResponsesForUserAsync(formId, userId))
                .ReturnsAsync(new List<ResponseDetailDTO>());

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this user.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetResponsesForForm_NullResponses_ReturnsNotFound()
        {
            // Arrange
            const string formId = "507f1f77bcf86cd799439011";
            const string userId = "user123";

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetResponsesForUserAsync(formId, userId))
                .ReturnsAsync((List<ResponseDetailDTO>)null!);

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this user.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetResponsesForForm_MultipleResponses_ReturnsAll()
        {
            // Arrange
            const string formId = "507f1f77bcf86cd799439011";
            const string userId = "user123";
            var expectedResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 1,
                    FormId = formId,
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    Answers = new List<ResponseAnswerDTO>()
                },
                new ResponseDetailDTO
                {
                    ResponseId = 2,
                    FormId = formId,
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
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
            Assert.Equal(2, returnedResponses.Count());
        }

        #endregion

        #region GetAllResponsesByLearner Tests

        [Fact]
        public async Task GetAllResponsesByLearner_UserHasResponses_ReturnsOkWithPaginatedData()
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

            var pagedResult = (expectedResponses, (long)expectedResponses.Count);
            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(
                userId,
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllResponsesByLearner(null, 1, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAllResponsesByLearner_WithSearch_ReturnsFilteredResults()
        {
            // Arrange
            const string userId = "user123";
            const string search = "Test Form";
            var filteredResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 1,
                    FormId = "form1",
                    FormTitle = "Test Form",
                    SubmittedBy = userId,
                    Answers = new List<ResponseAnswerDTO>()
                }
            };

            SetupUserIdentity(_controller, userId);

            var pagedResult = (filteredResponses, (long)filteredResponses.Count);
            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(
                userId,
                search,
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllResponsesByLearner(search, 1, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockResponseBL.Verify(bl => bl.GetAllResponsesByUserAsync(
                userId,
                search,
                1,
                6),
                Times.Once);
        }

        [Fact]
        public async Task GetAllResponsesByLearner_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            const string userId = "user123";
            var responses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 3,
                    FormId = "form3",
                    SubmittedBy = userId,
                    Answers = new List<ResponseAnswerDTO>()
                }
            };

            SetupUserIdentity(_controller, userId);

            var pagedResult = (responses, 10L);
            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(
                userId,
                It.IsAny<string?>(),
                2,
                6))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllResponsesByLearner(null, 2, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockResponseBL.Verify(bl => bl.GetAllResponsesByUserAsync(
                userId,
                null,
                2,
                6),
                Times.Once);
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
            var result = await _controller.GetAllResponsesByLearner(null, 1, 6);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);

            var resultDict = JObject.FromObject(unauthorizedResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("User ID not found in token.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetAllResponsesByLearner_NoResponses_ReturnsNotFound()
        {
            // Arrange
            const string userId = "user123";

            SetupUserIdentity(_controller, userId);

            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(
                userId,
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.GetAllResponsesByLearner(null, 1, 6);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this user.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetAllResponsesByLearner_DefaultPagination_UsesCorrectDefaults()
        {
            // Arrange
            const string userId = "user123";
            var responses = new List<ResponseDetailDTO>();

            SetupUserIdentity(_controller, userId);

            var pagedResult = (responses, 0L);
            _mockResponseBL.Setup(bl => bl.GetAllResponsesByUserAsync(
                userId,
                It.IsAny<string?>(),
                1,
                6))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllResponsesByLearner(null, 1, 6);

            // Assert
            _mockResponseBL.Verify(bl => bl.GetAllResponsesByUserAsync(
                userId,
                null,
                1,
                6),
                Times.Once);
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
                .ReturnsAsync((ResponseFileDTO)null!);

            // Act
            var result = await _controller.DownloadFile(responseId, fileId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("File not found for this response.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task DownloadFile_ImageFile_ReturnsCorrectContentType()
        {
            // Arrange
            const int responseId = 1;
            const int fileId = 2;
            var fileDto = new ResponseFileDTO
            {
                FileName = "image.png",
                FileType = "image/png",
                Base64Content = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            };

            _mockResponseBL.Setup(bl => bl.GetFileByResponseIdAndFileIdAsync(responseId, fileId))
                .ReturnsAsync(fileDto);

            // Act
            var result = await _controller.DownloadFile(responseId, fileId);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", fileResult.ContentType);
            Assert.Equal("image.png", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_VerifiesCorrectParameters()
        {
            // Arrange
            const int responseId = 123;
            const int fileId = 456;

            _mockResponseBL.Setup(bl => bl.GetFileByResponseIdAndFileIdAsync(responseId, fileId))
                .ReturnsAsync((ResponseFileDTO)null!);

            // Act
            await _controller.DownloadFile(responseId, fileId);

            // Assert
            _mockResponseBL.Verify(bl => bl.GetFileByResponseIdAndFileIdAsync(responseId, fileId), Times.Once);
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
