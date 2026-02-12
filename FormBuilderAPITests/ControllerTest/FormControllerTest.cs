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

        #region Constructor Tests

        [Fact]
        public void Constructor_NullFormBL_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormController(null!));
        }

        #endregion

        #region GetAllForms Tests

        [Fact]
        public async Task GetAllForms_AsAdmin_ReturnsAllFormsWithPagination()
        {
            var expectedForms = new List<Form>
            {
                new Form
                {
                    Id = "form1",
                    Config = new FormConfig { Title = "Form 1", Description = "Description 1" },
                    Status = MongoFormStatus.Published,
                    CreatedAt = DateTime.UtcNow
                },
                new Form
                {
                    Id = "form2",
                    Config = new FormConfig { Title = "Form 2", Description = "Description 2" },
                    Status = MongoFormStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                }
            };

            int pageNumber = 1;
            int pageSize = 9;
            long totalCount = 2;

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Admin", pageNumber, pageSize, null))
                .ReturnsAsync((expectedForms, totalCount));

            var result = await _controller.GetAllForms(pageNumber, pageSize, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);

            Assert.True(resultDict.ContainsKey("TotalCount"));
            Assert.True(resultDict.ContainsKey("PageNumber"));
            Assert.True(resultDict.ContainsKey("PageSize"));
            Assert.True(resultDict.ContainsKey("TotalPages"));
            Assert.True(resultDict.ContainsKey("Search"));
            Assert.True(resultDict.ContainsKey("Data"));

            Assert.Equal(totalCount, resultDict["TotalCount"]!.Value<long>());
            Assert.Equal(pageNumber, resultDict["PageNumber"]!.Value<int>());
            Assert.Equal(pageSize, resultDict["PageSize"]!.Value<int>());
            Assert.Equal(1, resultDict["TotalPages"]!.Value<int>());

            var returnedForms = resultDict["Data"]!.ToObject<List<Form>>();
            Assert.Equal(expectedForms.Count, returnedForms!.Count);
        }

        [Fact]
        public async Task GetAllForms_AsLearner_ReturnsPublishedFormsOnly()
        {
            var expectedForms = new List<Form>
            {
                new Form
                {
                    Id = "form1",
                    Config = new FormConfig { Title = "Form 1", Description = "Description 1" },
                    Status = MongoFormStatus.Published,
                    CreatedAt = DateTime.UtcNow
                }
            };

            int pageNumber = 1;
            int pageSize = 9;
            long totalCount = 1;

            SetupUserIdentity(_controller, "learner123", new[] { "Learner" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Learner", pageNumber, pageSize, null))
                .ReturnsAsync((expectedForms, totalCount));

            var result = await _controller.GetAllForms(pageNumber, pageSize, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);

            Assert.Equal(totalCount, resultDict["TotalCount"]!.Value<long>());

            var returnedForms = resultDict["Data"]!.ToObject<List<Form>>();
            Assert.Single(returnedForms!);
            Assert.All(returnedForms, f => Assert.Equal(MongoFormStatus.Published, f.Status));
        }

        [Fact]
        public async Task GetAllForms_WithSearch_ReturnsFilteredForms()
        {
            var expectedForms = new List<Form>
            {
                new Form
                {
                    Id = "form1",
                    Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                    Status = MongoFormStatus.Published,
                    CreatedAt = DateTime.UtcNow
                }
            };

            int pageNumber = 1;
            int pageSize = 9;
            string search = "Test";
            long totalCount = 1;

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Admin", pageNumber, pageSize, search))
                .ReturnsAsync((expectedForms, totalCount));

            var result = await _controller.GetAllForms(pageNumber, pageSize, search);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);

            Assert.Equal(search, resultDict["Search"]!.Value<string>());
            Assert.Equal(totalCount, resultDict["TotalCount"]!.Value<long>());

            _mockFormBL.Verify(bl => bl.GetAllFormsAsync("Admin", pageNumber, pageSize, search), Times.Once);
        }

        [Fact]
        public async Task GetAllForms_WithCustomPagination_ReturnsCorrectPage()
        {
            var expectedForms = new List<Form>
            {
                new Form
                {
                    Id = "form3",
                    Config = new FormConfig { Title = "Form 3", Description = "Description 3" },
                    Status = MongoFormStatus.Published,
                    CreatedAt = DateTime.UtcNow
                }
            };

            int pageNumber = 2;
            int pageSize = 5;
            long totalCount = 10;

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Admin", pageNumber, pageSize, null))
                .ReturnsAsync((expectedForms, totalCount));

            var result = await _controller.GetAllForms(pageNumber, pageSize, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);

            Assert.Equal(totalCount, resultDict["TotalCount"]!.Value<long>());
            Assert.Equal(pageNumber, resultDict["PageNumber"]!.Value<int>());
            Assert.Equal(pageSize, resultDict["PageSize"]!.Value<int>());
            Assert.Equal(2, resultDict["TotalPages"]!.Value<int>());
        }

        [Fact]
        public async Task GetAllForms_InvalidPageNumber_UsesDefaultValue()
        {
            var expectedForms = new List<Form>();
            long totalCount = 0;

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Admin", 1, 9, null))
                .ReturnsAsync((expectedForms, totalCount));

            var result = await _controller.GetAllForms(0, 9, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);

            Assert.Equal(1, resultDict["PageNumber"]!.Value<int>());
        }

        [Fact]
        public async Task GetAllForms_InvalidPageSize_UsesDefaultValue()
        {
            var expectedForms = new List<Form>();
            long totalCount = 0;

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetAllFormsAsync("Admin", 1, 9, null))
                .ReturnsAsync((expectedForms, totalCount));

            var result = await _controller.GetAllForms(1, 0, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);

            Assert.Equal(9, resultDict["PageSize"]!.Value<int>());
        }

        #endregion

        #region GetFormById Tests

        [Fact]
        public async Task GetFormById_AdminRole_FormExists_ReturnsForm()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var expectedForm = new Form
            {
                Id = formId,
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Status = MongoFormStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetFormByIdAsync(formId, "Admin"))
                .ReturnsAsync(expectedForm);

            var result = await _controller.GetFormById(formId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForm = Assert.IsType<Form>(okResult.Value);
            Assert.Equal(formId, returnedForm.Id);
            Assert.Equal(expectedForm.Config.Title, returnedForm.Config.Title);
        }

        [Fact]
        public async Task GetFormById_LearnerRole_PublishedForm_ReturnsForm()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var expectedForm = new Form
            {
                Id = formId,
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Status = MongoFormStatus.Published,
                CreatedAt = DateTime.UtcNow
            };

            SetupUserIdentity(_controller, "learner123", new[] { "Learner" });

            _mockFormBL.Setup(bl => bl.GetFormByIdAsync(formId, "Learner"))
                .ReturnsAsync(expectedForm);

            var result = await _controller.GetFormById(formId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForm = Assert.IsType<Form>(okResult.Value);
            Assert.Equal(MongoFormStatus.Published, returnedForm.Status);
        }

        [Fact]
        public async Task GetFormById_FormDoesNotExist_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.GetFormByIdAsync(formId, "Admin"))
                .ReturnsAsync((Form?)null);

            var result = await _controller.GetFormById(formId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form not found or not accessible.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task GetFormById_LearnerRole_DraftForm_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";

            SetupUserIdentity(_controller, "learner123", new[] { "Learner" });

            _mockFormBL.Setup(bl => bl.GetFormByIdAsync(formId, "Learner"))
                .ReturnsAsync((Form?)null);

            var result = await _controller.GetFormById(formId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.Equal("Form not found or not accessible.", resultDict["message"]!.Value<string>());
        }

        #endregion

        #region CreateFormConfig Tests

        [Fact]
        public async Task CreateFormConfig_ValidConfig_ReturnsCreatedResult()
        {
            const string newFormId = "507f1f77bcf86cd799439011";
            const string userName = "admin123";
            var configDto = new FormConfigDTO
            {
                Title = "New Form",
                Description = "Form Description"
            };

            SetupUserIdentity(_controller, "user123", new[] { "Admin" }, userName);

            _mockFormBL.Setup(bl => bl.CreateFormConfigAsync(configDto, userName))
                .ReturnsAsync(newFormId);

            var result = await _controller.CreateFormConfig(configDto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(FormController.GetFormById), createdResult.ActionName);
            Assert.Equal(newFormId, createdResult.RouteValues!["id"]);

            var resultDict = JObject.FromObject(createdResult.Value!);
            Assert.True(resultDict.ContainsKey("id"));
            Assert.Equal(newFormId, resultDict["id"]!.Value<string>());
        }

        [Fact]
        public async Task CreateFormConfig_InvalidModel_ReturnsBadRequest()
        {
            var configDto = new FormConfigDTO();

            SetupUserIdentity(_controller, "user123", new[] { "Admin" }, "admin123");

            _controller.ModelState.AddModelError("Title", "Title is required");

            var result = await _controller.CreateFormConfig(configDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateFormConfig_NoUserName_ThrowsArgumentException()
        {
            var configDto = new FormConfigDTO
            {
                Title = "New Form",
                Description = "Description"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user123"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.CreateFormConfig(configDto));
        }

        #endregion

        #region UpdateFormConfig Tests

        [Fact]
        public async Task UpdateFormConfig_ValidConfig_ReturnsOk()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var configDto = new FormConfigDTO
            {
                Title = "Updated Form",
                Description = "Updated Description"
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormConfigAsync(formId, configDto))
                .ReturnsAsync(true);

            var result = await _controller.UpdateFormConfig(formId, configDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);
            Assert.True(resultDict.ContainsKey("message"));
            Assert.Equal("Form configuration updated successfully.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task UpdateFormConfig_FormNotFound_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var configDto = new FormConfigDTO
            {
                Title = "Updated Form",
                Description = "Updated Description"
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormConfigAsync(formId, configDto))
                .ReturnsAsync(false);

            var result = await _controller.UpdateFormConfig(formId, configDto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.Equal("Form not found.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task UpdateFormConfig_PublishedForm_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var configDto = new FormConfigDTO
            {
                Title = "Updated Form",
                Description = "Updated Description"
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormConfigAsync(formId, configDto))
                .ThrowsAsync(new InvalidOperationException("Cannot edit a published form."));

            var result = await _controller.UpdateFormConfig(formId, configDto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultDict = JObject.FromObject(badRequestResult.Value!);
            Assert.Equal("Cannot edit a published form.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task UpdateFormConfig_InvalidModel_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var configDto = new FormConfigDTO();

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _controller.ModelState.AddModelError("Title", "Title is required");

            var result = await _controller.UpdateFormConfig(formId, configDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region CreateFormLayout Tests

        [Fact]
        public async Task CreateFormLayout_ValidLayout_ReturnsOk()
        {
            const string formId = "507f1f77bcf86cd799439011";
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
                        Label = "Question 1",
                        Type = "text",
                        Required = true,
                        Order = 1
                    }
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ReturnsAsync(true);

            var result = await _controller.CreateFormLayout(formId, layoutDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);
            Assert.Equal("Form layout created successfully.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task CreateFormLayout_FormNotFound_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Form Header",
                    Description = "Form Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ThrowsAsync(new Exception("Form not found."));

            var result = await _controller.CreateFormLayout(formId, layoutDto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.Equal("Form not found.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task CreateFormLayout_CreationFailed_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Form Header",
                    Description = "Form Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ReturnsAsync(false);

            var result = await _controller.CreateFormLayout(formId, layoutDto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultDict = JObject.FromObject(badRequestResult.Value!);
            Assert.Equal("Form layout creation failed.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task CreateFormLayout_InvalidModel_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO();

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _controller.ModelState.AddModelError("HeaderCard", "HeaderCard is required");

            var result = await _controller.CreateFormLayout(formId, layoutDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateFormLayout_PublishedForm_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Form Header",
                    Description = "Form Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.CreateFormLayoutAsync(formId, layoutDto))
                .ThrowsAsync(new InvalidOperationException("Cannot add layout to a published form."));

            var result = await _controller.CreateFormLayout(formId, layoutDto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultDict = JObject.FromObject(badRequestResult.Value!);
            Assert.Equal("Cannot add layout to a published form.", resultDict["message"]!.Value<string>());
        }

        #endregion

        #region UpdateFormLayout Tests

        [Fact]
        public async Task UpdateFormLayout_ValidLayout_ReturnsOk()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Updated Header",
                    Description = "Updated Description"
                },
                Fields = new List<FormFieldDTO>
                {
                    new FormFieldDTO
                    {
                        Label = "Updated Question",
                        Type = "text",
                        Required = true,
                        Order = 1
                    }
                }
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormLayoutAsync(formId, layoutDto))
                .ReturnsAsync(true);

            var result = await _controller.UpdateFormLayout(formId, layoutDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);
            Assert.Equal("Form layout updated successfully.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task UpdateFormLayout_FormNotFound_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Updated Header",
                    Description = "Updated Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormLayoutAsync(formId, layoutDto))
                .ReturnsAsync(false);

            var result = await _controller.UpdateFormLayout(formId, layoutDto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.Equal("Form not found.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task UpdateFormLayout_PublishedForm_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Updated Header",
                    Description = "Updated Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.UpdateFormLayoutAsync(formId, layoutDto))
                .ThrowsAsync(new InvalidOperationException("Cannot edit a published form."));

            var result = await _controller.UpdateFormLayout(formId, layoutDto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultDict = JObject.FromObject(badRequestResult.Value!);
            Assert.Equal("Cannot edit a published form.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task UpdateFormLayout_InvalidModel_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var layoutDto = new FormLayoutDTO();

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _controller.ModelState.AddModelError("HeaderCard", "HeaderCard is required");

            var result = await _controller.UpdateFormLayout(formId, layoutDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region PublishForm Tests

        [Fact]
        public async Task PublishForm_ValidForm_ReturnsOk()
        {
            const string formId = "507f1f77bcf86cd799439011";
            var publishedForm = new Form
            {
                Id = formId,
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Status = MongoFormStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.PublishFormAsync(formId))
                .ReturnsAsync(publishedForm);

            var result = await _controller.PublishForm(formId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedForm = Assert.IsType<Form>(okResult.Value);
            Assert.Equal(MongoFormStatus.Published, returnedForm.Status);
            Assert.NotNull(returnedForm.PublishedAt);
        }

        [Fact]
        public async Task PublishForm_AlreadyPublished_ReturnsBadRequest()
        {
            const string formId = "507f1f77bcf86cd799439011";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.PublishFormAsync(formId))
                .ThrowsAsync(new InvalidOperationException("Form is already published."));

            var result = await _controller.PublishForm(formId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultDict = JObject.FromObject(badRequestResult.Value!);
            Assert.Equal("Form is already published.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task PublishForm_FormNotFound_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.PublishFormAsync(formId))
                .ThrowsAsync(new Exception("Form not found."));

            var result = await _controller.PublishForm(formId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.Equal("Form not found.", resultDict["message"]!.Value<string>());
        }

        #endregion

        #region DeleteForm Tests

        [Fact]
        public async Task DeleteForm_FormExists_ReturnsOk()
        {
            const string formId = "507f1f77bcf86cd799439011";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.DeleteFormAsync(formId))
                .ReturnsAsync(true);

            var result = await _controller.DeleteForm(formId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultDict = JObject.FromObject(okResult.Value!);
            Assert.Equal("Form deleted successfully.", resultDict["message"]!.Value<string>());
        }

        [Fact]
        public async Task DeleteForm_FormNotFound_ReturnsNotFound()
        {
            const string formId = "507f1f77bcf86cd799439011";

            SetupUserIdentity(_controller, "admin123", new[] { "Admin" });

            _mockFormBL.Setup(bl => bl.DeleteFormAsync(formId))
                .ReturnsAsync(false);

            var result = await _controller.DeleteForm(formId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var resultDict = JObject.FromObject(notFoundResult.Value!);
            Assert.Equal("Form not found.", resultDict["message"]!.Value<string>());
        }

        #endregion

        #region Helper Methods

        private void SetupUserIdentity(ControllerBase controller, string userId, string[] roles, string? userName = null)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId)
    };

            if (userName != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, userName));
            }

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