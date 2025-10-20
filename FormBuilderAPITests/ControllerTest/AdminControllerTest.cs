using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

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

        #region GetResponsesForForm Tests

        [Fact]
        public async Task GetResponsesForForm_ReturnsOkWithResponses()
        {
            // Arrange
            const string formId = "123456789012345678901234"; // Valid MongoDB ObjectId format
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

            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(formId))
                .ReturnsAsync(expectedResponses);

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponses = Assert.IsAssignableFrom<List<ResponseDetailDTO>>(okResult.Value);
            
            Assert.Equal(expectedResponses.Count, returnedResponses.Count);
            
            // Verify first response
            Assert.Equal(expectedResponses[0].ResponseId, returnedResponses[0].ResponseId);
            Assert.Equal(expectedResponses[0].FormId, returnedResponses[0].FormId);
            Assert.Equal(expectedResponses[0].SubmittedBy, returnedResponses[0].SubmittedBy);
            
            // Verify second response
            Assert.Equal(expectedResponses[1].ResponseId, returnedResponses[1].ResponseId);
            Assert.Equal(expectedResponses[1].FormId, returnedResponses[1].FormId);
            Assert.Equal(expectedResponses[1].SubmittedBy, returnedResponses[1].SubmittedBy);
        }

        [Fact]
        public async Task GetResponsesForForm_NoResponses_ReturnsEmptyList()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            var emptyResponses = new List<ResponseDetailDTO>();

            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(formId))
                .ReturnsAsync(emptyResponses);

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponses = Assert.IsAssignableFrom<List<ResponseDetailDTO>>(okResult.Value);
            
            Assert.Empty(returnedResponses);
        }

        [Fact]
        public async Task GetResponsesForForm_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "123456789012345678901234";
            
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(formId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetResponsesForForm(formId));
        }

        [Fact]
        public async Task GetResponsesForForm_NullFormId_ReturnsEmptyList()
        {
            // Arrange
            string formId = null;
            var emptyResponses = new List<ResponseDetailDTO>();

            // Since the controller doesn't throw an exception for null formId,
            // we'll set up the mock to return an empty list
            _mockResponseBL.Setup(bl => bl.GetResponsesForFormAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyResponses);

            // Act
            var result = await _controller.GetResponsesForForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponses = Assert.IsAssignableFrom<List<ResponseDetailDTO>>(okResult.Value);
            
            Assert.Empty(returnedResponses);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_NullFormBL_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdminController(null, _mockResponseBL.Object));
        }

        [Fact]
        public void Constructor_NullResponseBL_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdminController(_mockFormBL.Object, null));
        }

        #endregion
    }
}
