using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Newtonsoft.Json.Linq;
using MongoFormStatus = FormBuilderAPI.Model.MongoModel.FormStatus;
using DTOFormStatus = FormBuilderAPI.DTOs.FormStatus;

namespace FormBuilderAPITests.ControllerTest
{
    public class FormControllerTest
    {
        private readonly Mock<IFormBL> _mockFormBL;
        private readonly FormController _controller;

        public FormControllerTest()
        {
            _mockFormBL = new Mock<IFormBL>();
            _controller = new FormController(_mockFormBL.Object);
        }

        #region GetAllForms Tests

        [Fact]
        public async Task GetAllForms_AsAdmin_ReturnsAllForms()
        {
            // Arrange
            var expectedForms = new List<Form>
            {
                new Form
                {
                    Id = "form1",
                    Config = new FormConfig { Title = "Form 1" },
                    Status = MongoFormStatus.Published
                },
                new Form
                {
                    Id = "form2",
                    Config = new FormConfig { Title = "Form 2" },
                    Status = MongoFormStatus.Draft
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Admin"))
                .ReturnsAsync(expectedForms);

            // Act
            var result = await _controller.GetAllForms();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForms = Assert.IsAssignableFrom<IEnumerable<Form>>(okResult.Value);
            Assert.Equal(expectedForms.Count, returnedForms.Count());
            Assert.Equal(expectedForms[0].Id, returnedForms.First().Id);
        }

        [Fact]
        public async Task GetAllForms_AsLearner_ReturnsPublishedForms()
        {
            // Arrange
            var expectedForms = new List<Form>
            {
                new Form
                {
                    Id = "form1",
                    Config = new FormConfig { Title = "Form 1" },
                    Status = MongoFormStatus.Published
                }
            };

            SetupUserIdentity(_controller, "learner123", new[] { "Learner" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Learner"))
                .ReturnsAsync(expectedForms);

            // Act
            var result = await _controller.GetAllForms();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForms = Assert.IsAssignableFrom<IEnumerable<Form>>(okResult.Value);
            Assert.Single(returnedForms);
            Assert.Equal(expectedForms[0].Id, returnedForms.First().Id);
        }

        #endregion

        #region GetFormById Tests

        [Fact]
        public async Task GetFormById_FormExists_ReturnsForm()
        {
            // Arrange
            const string formId = "form123";
            var expectedForm = new Form
            {
                Id = formId,
                Config = new FormConfig { Title = "Test Form" },
                Status = MongoFormStatus.Published
            };

            SetupUserIdentity(_controller, "user123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetFormByIdAsync(formId, "Admin"))
                .ReturnsAsync(expectedForm);

            // Act
            var result = await _controller.GetFormById(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForm = Assert.IsType<Form>(okResult.Value);
            Assert.Equal(formId, returnedForm.Id);
            Assert.Equal(expectedForm.Config.Title, returnedForm.Config.Title);
        }

        [Fact]
        public async Task GetFormById_FormDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            const string formId = "nonexistentform";

            SetupUserIdentity(_controller, "user123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetFormByIdAsync(formId, "Admin"))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _controller.GetFormById(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultValue = notFoundResult.Value;
            Assert.NotNull(resultValue);
            
            var resultDict = JObject.FromObject(resultValue);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form not found or not accessible.", resultDict["message"].Value<string>());
        }

        #endregion

        #region CreateFormConfig Tests

        [Fact]
        public async Task CreateFormConfig_ValidConfig_ReturnsCreatedResult()
        {
            // Arrange
            const string newFormId = "newform123";
            var configDto = new FormConfigDTO
            {
                Title = "New Form",
                Description = "Form Description"
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormConfigAsync(It.IsAny<FormConfigDTO>(), It.IsAny<string>()))
                .ReturnsAsync(newFormId);

            // Act
            var result = await _controller.CreateFormConfig(configDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(FormController.GetFormById), createdResult.ActionName);
            Assert.Equal(newFormId, createdResult.RouteValues["id"]);
            
            Assert.NotNull(createdResult.Value);
            var resultDict = JObject.FromObject(createdResult.Value);
            Assert.True(resultDict.ContainsKey("id"));
            Assert.Equal(newFormId, resultDict["id"].Value<string>());
        }

        [Fact]
        public async Task CreateFormConfig_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var configDto = new FormConfigDTO(); // Missing required Title
            
            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });
            
            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _controller.CreateFormConfig(configDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region CreateFormLayout Tests

        [Fact]
        public async Task CreateFormLayout_ValidLayout_ReturnsNoContent()
        {
            // Arrange
            const string formId = "form123";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Form Header",
                    Description = "Form Description"
                },
                Fields = new List<FormFieldDTO>
                {
                    new FormFieldDTO
                    {
                        QuestionId = "q1",
                        Label = "Question 1",
                        Type = "text",
                        Required = true
                    }
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateFormLayout(formId, layoutDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task CreateFormLayout_FormNotFound_ReturnsNotFound()
        {
            // Arrange
            const string formId = "nonexistentform";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Form Header"
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CreateFormLayout(formId, layoutDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form not found or layout not updated.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task CreateFormLayout_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "form123";
            var layoutDto = new FormLayoutDTO(); // Missing required HeaderCard
            
            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });
            
            _controller.ModelState.AddModelError("HeaderCard", "HeaderCard is required");

            // Act
            var result = await _controller.CreateFormLayout(formId, layoutDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateFormLayout_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "form123";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Form Header"
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ThrowsAsync(new Exception("Layout validation failed"));

            // Act
            var result = await _controller.CreateFormLayout(formId, layoutDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            
            var resultDict = JObject.FromObject(badRequestResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Layout validation failed", resultDict["message"].Value<string>());
        }

        #endregion

        #region UpdateForm Tests

        [Fact]
        public async Task UpdateForm_ValidForm_ReturnsOk()
        {
            // Arrange
            const string formId = "form123";
            var formDto = new FormDTO
            {
                Id = formId,
                Config = new FormConfigDTO { Title = "Updated Form" },
                Layout = new FormLayoutDTO
                {
                    HeaderCard = new FormHeaderCardDTO { Title = "Updated Header" }
                },
                Status = DTOFormStatus.Draft
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormAsync(formId, formDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateForm(formId, formDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var resultDict = JObject.FromObject(okResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form updated successfully.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task UpdateForm_FormNotFound_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "nonexistentform";
            var formDto = new FormDTO
            {
                Id = formId,
                Config = new FormConfigDTO { Title = "Updated Form" },
                Layout = new FormLayoutDTO
                {
                    HeaderCard = new FormHeaderCardDTO { Title = "Updated Header" }
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormAsync(formId, formDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateForm(formId, formDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            
            var resultDict = JObject.FromObject(badRequestResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Cannot edit a published form or form not found.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task UpdateForm_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "form123";
            var formDto = new FormDTO(); // Missing required properties
            
            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });
            
            _controller.ModelState.AddModelError("Config", "Config is required");

            // Act
            var result = await _controller.UpdateForm(formId, formDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region DeleteForm Tests

        [Fact]
        public async Task DeleteForm_FormExists_ReturnsOk()
        {
            // Arrange
            const string formId = "form123";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.DeleteFormAsync(formId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var resultDict = JObject.FromObject(okResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form deleted successfully.", resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task DeleteForm_FormNotFound_ReturnsNotFound()
        {
            // Arrange
            const string formId = "nonexistentform";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.DeleteFormAsync(formId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form not found.", resultDict["message"].Value<string>());
        }

        #endregion

        #region PublishForm Tests

        [Fact]
        public async Task PublishForm_ValidForm_ReturnsOk()
        {
            // Arrange
            const string formId = "form123";
            var publishedForm = new Form
            {
                Id = formId,
                Status = MongoFormStatus.Published,
                PublishedAt = DateTime.UtcNow
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.PublishFormAsync(formId))
                .ReturnsAsync(publishedForm);

            // Act
            var result = await _controller.PublishForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForm = Assert.IsType<Form>(okResult.Value);
            Assert.Equal(MongoFormStatus.Published, returnedForm.Status);
            Assert.NotNull(returnedForm.PublishedAt);
        }

        [Fact]
        public async Task PublishForm_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            const string formId = "form123";
            const string errorMessage = "Form is already published.";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.PublishFormAsync(formId))
                .ThrowsAsync(new InvalidOperationException(errorMessage));

            // Act
            var result = await _controller.PublishForm(formId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            
            var resultDict = JObject.FromObject(badRequestResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal(errorMessage, resultDict["message"].Value<string>());
        }

        [Fact]
        public async Task PublishForm_FormNotFound_ReturnsNotFound()
        {
            // Arrange
            const string formId = "nonexistentform";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.PublishFormAsync(formId))
                .ThrowsAsync(new Exception("Form not found"));

            // Act
            var result = await _controller.PublishForm(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            
            var resultDict = JObject.FromObject(notFoundResult.Value);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form not found.", resultDict["message"].Value<string>());
        }

        #endregion

        #region Helper Methods

        private void SetupUserIdentity(ControllerBase controller, string userId, string[] roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "Test User")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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
