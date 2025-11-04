using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Newtonsoft.Json.Linq;

namespace FormBuilderAPITests.ControllerTest
{
    public class AdminControllerTest
    {
        private readonly Mock<IFormBL> _mockFormBL;
        private readonly Mock<IResponseBL> _mockResponseBL;
        private readonly AdminController _controller;

        public AdminControllerTest()
        {
            _mockFormBL = new Mock<IFormBL>();
            _mockResponseBL = new Mock<IResponseBL>();
            _controller = new AdminController(_mockFormBL.Object, _mockResponseBL.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullFormBL_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdminController(null!, _mockResponseBL.Object));
        }

        [Fact]
        public void Constructor_NullResponseBL_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdminController(_mockFormBL.Object, null!));
        }

        #endregion

        #region GetResponsesForForm Tests

        [Fact]
        public async Task GetResponsesForForm_ReturnsOkWithResponses()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            var expectedResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 1,
                    FormId = formId,
                    SubmittedBy = "user123",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Answers = new List<ResponseAnswerDTO>
                    {
                        new ResponseAnswerDTO
                        {
                            AnswerId = 1,
                            QuestionId = "q1",
                            AnswerText = "Answer 1"
                        }
                    }
                },
                new ResponseDetailDTO
                {
                    ResponseId = 2,
                    FormId = formId,
                    SubmittedBy = "user456",
                    SubmittedAt = DateTime.UtcNow.AddHours(-5),
                    Answers = new List<ResponseAnswerDTO>
                    {
                        new ResponseAnswerDTO
                        {
                            AnswerId = 2,
                            QuestionId = "q1",
                            AnswerText = "Answer 2"
                        }
                    }
                }
            };

            // Setup mock to return paginated result (tuple)
            var pagedResult = (expectedResponses, (long)expectedResponses.Count);
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(
                formId,
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetResponsesForForm(formId, null, 1, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify the mock was called with correct parameters
            _mockResponseBL.Verify(bl => bl.GetResponsesForFormAsync(
                formId,
                null,
                1,
                6),
                Times.Once);
        }

        [Fact]
        public async Task GetResponsesForForm_NoResponses_ReturnsNotFound()
        {
            // Arrange
            const string formId = "123456789012345678901234";

            // Setup mock to return null (no responses found)
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(
                formId,
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.GetResponsesForForm(formId, null, 1, 6);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this form.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetResponsesForForm_WithSearch_ReturnsFilteredResponses()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            const string search = "user123";
            var filteredResponses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 1,
                    FormId = formId,
                    SubmittedBy = "user123",
                    SubmittedUserName = "John Doe",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<ResponseAnswerDTO>()
                }
            };

            var pagedResult = (filteredResponses, (long)filteredResponses.Count);
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(
                formId,
                search,
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetResponsesForForm(formId, search, 1, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockResponseBL.Verify(bl => bl.GetResponsesForFormAsync(
                formId,
                search,
                1,
                6),
                Times.Once);
        }

        [Fact]
        public async Task GetResponsesForForm_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            var responses = new List<ResponseDetailDTO>
            {
                new ResponseDetailDTO
                {
                    ResponseId = 7,
                    FormId = formId,
                    SubmittedBy = "user7",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<ResponseAnswerDTO>()
                }
            };

            var pagedResult = (responses, 10L); // 10 total, but only 1 on this page
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(
                formId,
                It.IsAny<string?>(),
                2,
                6))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetResponsesForForm(formId, null, 2, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockResponseBL.Verify(bl => bl.GetResponsesForFormAsync(
                formId,
                null,
                2,
                6),
                Times.Once);
        }

        [Fact]
        public async Task GetResponsesForForm_DefaultPagination_UsesCorrectDefaults()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            var responses = new List<ResponseDetailDTO>();

            var pagedResult = (responses, 0L);
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(
                formId,
                It.IsAny<string?>(),
                1,
                6))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetResponsesForForm(formId, null, 1, 6);


            // Assert
            _mockResponseBL.Verify(bl => bl.GetResponsesForFormAsync(
                formId,
                null,
                1,
                6),
                Times.Once);
        }

        [Fact]
        public async Task GetResponsesForForm_EmptyResponsesList_ReturnsNotFound()
        {
            // Arrange
            const string formId = "123456789012345678901234";

            // Return null to indicate no responses found
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(
                formId,
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.GetResponsesForForm(formId, null, 1, 6);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("No responses found for this form.", resultDict["message"]!.Value<string>());
        }

        #endregion
    }
}
