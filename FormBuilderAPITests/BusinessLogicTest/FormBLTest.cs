using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using FormBuilderAPI.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MongoFormStatus = FormBuilderAPI.Model.MongoModel.FormStatus;

namespace FormBuilderAPITests.BusinessLogicTest
{
    public class FormBLTest
    {
        private readonly Mock<IFormRepository> _mockFormRepository;
        private readonly IFormBL _formBL;

        public FormBLTest()
        {
            _mockFormRepository = new Mock<IFormRepository>();
            _formBL = new FormBL(_mockFormRepository.Object);
        }

        #region GetAllFormsAsync Tests

        [Fact]
        public async Task GetAllFormsAsync_AdminRole_ReturnsAllForms()
        {
            // Arrange
            var forms = new List<Form>
            {
                CreateTestForm("1", MongoFormStatus.Draft, "Draft Form"),
                CreateTestForm("2", MongoFormStatus.Published, "Published Form"),
                CreateTestForm("3", MongoFormStatus.Draft, "Another Draft")
            };

            _mockFormRepository
                .Setup(repo => repo.GetAllFormsAsync("Admin", 1, 10, null))
                .ReturnsAsync((forms, 3L));

            // Act
            var (result, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(3, result.Count());
            _mockFormRepository.Verify(repo => repo.GetAllFormsAsync("Admin", 1, 10, null), Times.Once);
        }

        [Fact]
        public async Task GetAllFormsAsync_LearnerRole_ReturnsOnlyPublishedForms()
        {
            // Arrange
            var publishedForms = new List<Form>
            {
                CreateTestForm("2", MongoFormStatus.Published, "Published Form")
            };

            _mockFormRepository
                .Setup(repo => repo.GetAllFormsAsync("Learner", 1, 10, null))
                .ReturnsAsync((publishedForms, 1L));

            // Act
            var (result, totalCount) = await _formBL.GetAllFormsAsync("Learner", 1, 10);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(result);
            Assert.Equal("2", result.First().Id);
            Assert.Equal(MongoFormStatus.Published, result.First().Status);
            _mockFormRepository.Verify(repo => repo.GetAllFormsAsync("Learner", 1, 10, null), Times.Once);
        }

        [Fact]
        public async Task GetAllFormsAsync_WithSearch_ReturnsFilteredForms()
        {
            // Arrange
            var filteredForms = new List<Form>
            {
                CreateTestForm("1", MongoFormStatus.Draft, "Draft Form"),
                CreateTestForm("3", MongoFormStatus.Draft, "Another Draft")
            };

            _mockFormRepository
                .Setup(repo => repo.GetAllFormsAsync("Admin", 1, 10, "Draft"))
                .ReturnsAsync((filteredForms, 2L));

            // Act
            var (result, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10, "Draft");

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, result.Count());
            _mockFormRepository.Verify(repo => repo.GetAllFormsAsync("Admin", 1, 10, "Draft"), Times.Once);
        }

        [Fact]
        public async Task GetAllFormsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var firstPageForms = new List<Form>
            {
                CreateTestForm("1", MongoFormStatus.Draft, "Draft Form"),
                CreateTestForm("2", MongoFormStatus.Published, "Published Form")
            };

            _mockFormRepository
                .Setup(repo => repo.GetAllFormsAsync("Admin", 1, 2, null))
                .ReturnsAsync((firstPageForms, 3L));

            // Act
            var (result, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 2);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(2, result.Count());
            _mockFormRepository.Verify(repo => repo.GetAllFormsAsync("Admin", 1, 2, null), Times.Once);
        }

        [Fact]
        public async Task GetAllFormsAsync_NoResults_ReturnsEmptyList()
        {
            // Arrange
            _mockFormRepository
                .Setup(repo => repo.GetAllFormsAsync("Learner", 1, 10, "NonExistent"))
                .ReturnsAsync((new List<Form>(), 0L));

            // Act
            var (result, totalCount) = await _formBL.GetAllFormsAsync("Learner", 1, 10, "NonExistent");

            // Assert
            Assert.Equal(0, totalCount);
            Assert.Empty(result);
            _mockFormRepository.Verify(repo => repo.GetAllFormsAsync("Learner", 1, 10, "NonExistent"), Times.Once);
        }

        #endregion

        #region GetFormByIdAsync Tests

        [Fact]
        public async Task GetFormByIdAsync_AdminRole_ReturnsForm()
        {
            // Arrange
            var form = CreateTestForm("1", MongoFormStatus.Draft, "Draft Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync("1", "Admin"))
                .ReturnsAsync(form);

            // Act
            var result = await _formBL.GetFormByIdAsync("1", "Admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync("1", "Admin"), Times.Once);
        }

        [Fact]
        public async Task GetFormByIdAsync_LearnerRole_PublishedForm_ReturnsForm()
        {
            // Arrange
            var form = CreateTestForm("2", MongoFormStatus.Published, "Published Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync("2", "Learner"))
                .ReturnsAsync(form);

            // Act
            var result = await _formBL.GetFormByIdAsync("2", "Learner");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2", result.Id);
            Assert.Equal(MongoFormStatus.Published, result.Status);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync("2", "Learner"), Times.Once);
        }

        [Fact]
        public async Task GetFormByIdAsync_LearnerRole_DraftForm_ReturnsNull()
        {
            // Arrange
            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync("1", "Learner"))
                .ReturnsAsync((Form?)null);

            // Act
            var result = await _formBL.GetFormByIdAsync("1", "Learner");

            // Assert
            Assert.Null(result);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync("1", "Learner"), Times.Once);
        }

        [Fact]
        public async Task GetFormByIdAsync_NonExistentForm_ReturnsNull()
        {
            // Arrange
            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync("999", "Admin"))
                .ReturnsAsync((Form?)null);

            // Act
            var result = await _formBL.GetFormByIdAsync("999", "Admin");

            // Assert
            Assert.Null(result);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync("999", "Admin"), Times.Once);
        }

        #endregion

        #region CreateFormConfigAsync Tests

        [Fact]
        public async Task CreateFormConfigAsync_ValidDto_ReturnsFormId()
        {
            // Arrange
            var dto = new FormConfigDTO
            {
                Title = "Test Form",
                Description = "Test Description"
            };
            var createdBy = "testuser";
            var expectedFormId = "generated-form-id";

            _mockFormRepository
                .Setup(repo => repo.InsertFormAsync(It.IsAny<Form>()))
                .ReturnsAsync(expectedFormId);

            // Act
            var result = await _formBL.CreateFormConfigAsync(dto, createdBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedFormId, result);
            _mockFormRepository.Verify(repo => repo.InsertFormAsync(It.Is<Form>(f =>
                f.Config.Title == "Test Form" &&
                f.Config.Description == "Test Description" &&
                f.Status == MongoFormStatus.Draft &&
                f.CreatedBy == createdBy
            )), Times.Once);
        }

        [Fact]
        public async Task CreateFormConfigAsync_CreatesFormWithDraftStatus()
        {
            // Arrange
            var dto = new FormConfigDTO
            {
                Title = "New Form",
                Description = "New Description"
            };
            var createdBy = "admin";
            var formId = "new-form-id";

            _mockFormRepository
                .Setup(repo => repo.InsertFormAsync(It.IsAny<Form>()))
                .ReturnsAsync(formId);

            // Act
            var result = await _formBL.CreateFormConfigAsync(dto, createdBy);

            // Assert
            Assert.Equal(formId, result);
            _mockFormRepository.Verify(repo => repo.InsertFormAsync(It.Is<Form>(f =>
                f.Status == MongoFormStatus.Draft &&
                f.CreatedBy == "admin"
            )), Times.Once);
        }

        #endregion

        #region UpdateFormConfigAsync Tests

        [Fact]
        public async Task UpdateFormConfigAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1";
            var dto = new FormConfigDTO
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };
            var existingForm = CreateTestForm(formId, MongoFormStatus.Draft, "Old Title");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            _mockFormRepository
                .Setup(repo => repo.UpdateFormConfigAsync(formId, dto.Title, dto.Description))
                .ReturnsAsync(true);

            // Act
            var result = await _formBL.UpdateFormConfigAsync(formId, dto);

            // Assert
            Assert.True(result);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormConfigAsync(formId, dto.Title, dto.Description), Times.Once);
        }

        [Fact]
        public async Task UpdateFormConfigAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent";
            var dto = new FormConfigDTO
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync((Form?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _formBL.UpdateFormConfigAsync(formId, dto));
            Assert.Equal("Form not found.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateFormConfigAsync_PublishedForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2";
            var dto = new FormConfigDTO
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };
            var publishedForm = CreateTestForm(formId, MongoFormStatus.Published, "Published Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(publishedForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.UpdateFormConfigAsync(formId, dto));
            Assert.Equal("Cannot edit a published form.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region CreateFormLayoutAsync Tests

        [Fact]
        public async Task CreateFormLayoutAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Header Title",
                    Description = "Header Description"
                },
                Fields = new List<FormFieldDTO>
                {
                    new FormFieldDTO
                    {
                        Label = "Question 1",
                        Type = "text",
                        Required = true,
                        Order = 1,
                        Options = new List<FieldOptionDTO>()
                    }
                }
            };
            var existingForm = CreateTestForm(formId, MongoFormStatus.Draft, "Draft Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            _mockFormRepository
                .Setup(repo => repo.UpdateFormLayoutAsync(formId, It.IsAny<FormLayout>()))
                .ReturnsAsync(true);

            // Act
            var result = await _formBL.CreateFormLayoutAsync(formId, layoutDto);

            // Assert
            Assert.True(result);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormLayoutAsync(formId, It.IsAny<FormLayout>()), Times.Once);
        }

        [Fact]
        public async Task CreateFormLayoutAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Header Title",
                    Description = "Header Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync((Form?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _formBL.CreateFormLayoutAsync(formId, layoutDto));
            Assert.Equal("Form not found.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormLayoutAsync(It.IsAny<string>(), It.IsAny<FormLayout>()), Times.Never);
        }

        [Fact]
        public async Task CreateFormLayoutAsync_PublishedForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Header Title",
                    Description = "Header Description"
                },
                Fields = new List<FormFieldDTO>()
            };
            var publishedForm = CreateTestForm(formId, MongoFormStatus.Published, "Published Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(publishedForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.CreateFormLayoutAsync(formId, layoutDto));
            Assert.Equal("Cannot add layout to a published form.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormLayoutAsync(It.IsAny<string>(), It.IsAny<FormLayout>()), Times.Never);
        }

        #endregion

        #region UpdateFormLayoutAsync Tests

        [Fact]
        public async Task UpdateFormLayoutAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1";
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
                        Order = 1,
                        Options = new List<FieldOptionDTO>()
                    }
                }
            };
            var existingForm = CreateTestForm(formId, MongoFormStatus.Draft, "Draft Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            _mockFormRepository
                .Setup(repo => repo.UpdateFormLayoutAsync(formId, It.IsAny<FormLayout>()))
                .ReturnsAsync(true);

            // Act
            var result = await _formBL.UpdateFormLayoutAsync(formId, layoutDto);

            // Assert
            Assert.True(result);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormLayoutAsync(formId, It.IsAny<FormLayout>()), Times.Once);
        }

        [Fact]
        public async Task UpdateFormLayoutAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Updated Header",
                    Description = "Updated Description"
                },
                Fields = new List<FormFieldDTO>()
            };

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync((Form?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _formBL.UpdateFormLayoutAsync(formId, layoutDto));
            Assert.Equal("Form not found.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormLayoutAsync(It.IsAny<string>(), It.IsAny<FormLayout>()), Times.Never);
        }

        [Fact]
        public async Task UpdateFormLayoutAsync_PublishedForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Updated Header",
                    Description = "Updated Description"
                },
                Fields = new List<FormFieldDTO>()
            };
            var publishedForm = CreateTestForm(formId, MongoFormStatus.Published, "Published Form");

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(publishedForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.UpdateFormLayoutAsync(formId, layoutDto));
            Assert.Equal("Cannot edit a published form.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormLayoutAsync(It.IsAny<string>(), It.IsAny<FormLayout>()), Times.Never);
        }

        #endregion

        #region PublishFormAsync Tests

        [Fact]
        public async Task PublishFormAsync_DraftForm_PublishesSuccessfully()
        {
            // Arrange
            var formId = "1";
            var draftForm = CreateTestForm(formId, MongoFormStatus.Draft, "Draft Form");
            var publishedForm = CreateTestForm(formId, MongoFormStatus.Published, "Draft Form");
            publishedForm.PublishedAt = DateTime.UtcNow;

            _mockFormRepository
                .SetupSequence(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(draftForm)
                .ReturnsAsync(publishedForm);

            _mockFormRepository
                .Setup(repo => repo.UpdateFormStatusAsync(formId, MongoFormStatus.Published, It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            // Act
            var result = await _formBL.PublishFormAsync(formId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(MongoFormStatus.Published, result.Status);
            Assert.NotNull(result.PublishedAt);
            _mockFormRepository.Verify(repo => repo.UpdateFormStatusAsync(formId, MongoFormStatus.Published, It.IsAny<DateTime>()), Times.Once);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Exactly(2));
        }

        [Fact]
        public async Task PublishFormAsync_AlreadyPublished_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2";
            var publishedForm = CreateTestForm(formId, MongoFormStatus.Published, "Published Form");
            publishedForm.PublishedAt = DateTime.UtcNow;

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(publishedForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.PublishFormAsync(formId));
            Assert.Equal("Form is already published.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormStatusAsync(It.IsAny<string>(), It.IsAny<MongoFormStatus>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task PublishFormAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent";

            _mockFormRepository
                .Setup(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync((Form?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _formBL.PublishFormAsync(formId));
            Assert.Equal("Form not found.", exception.Message);
            _mockFormRepository.Verify(repo => repo.GetFormByIdAsync(formId), Times.Once);
            _mockFormRepository.Verify(repo => repo.UpdateFormStatusAsync(It.IsAny<string>(), It.IsAny<MongoFormStatus>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task PublishFormAsync_FailsToRetrieveAfterUpdate_ThrowsException()
        {
            // Arrange
            var formId = "1";
            var draftForm = CreateTestForm(formId, MongoFormStatus.Draft, "Draft Form");

            _mockFormRepository
                .SetupSequence(repo => repo.GetFormByIdAsync(formId))
                .ReturnsAsync(draftForm)
                .ReturnsAsync((Form?)null);

            _mockFormRepository
                .Setup(repo => repo.UpdateFormStatusAsync(formId, MongoFormStatus.Published, It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _formBL.PublishFormAsync(formId));
            Assert.Equal("Failed to retrieve published form.", exception.Message);
        }

        #endregion

        #region Helper Methods

        private Form CreateTestForm(string id, MongoFormStatus status, string title)
        {
            return new Form
            {
                Id = id,
                Status = status,
                Config = new FormConfig
                {
                    Title = title,
                    Description = $"{title} Description"
                },
                Layout = new FormLayout
                {
                    HeaderCard = new FormHeaderCard
                    {
                        Title = $"{title} Header",
                        Description = $"{title} Header Description"
                    },
                    Fields = new List<FormField>()
                },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin",
                PublishedAt = status == MongoFormStatus.Published ? DateTime.UtcNow : null
            };
        }

        #endregion
    }
}
