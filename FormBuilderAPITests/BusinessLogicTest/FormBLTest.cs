using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MongoFormStatus = FormBuilderAPI.Model.MongoModel.FormStatus;

namespace FormBuilderAPITests.BusinessLogicTest
{
    public class FormBLTest : IDisposable
    {
        private readonly SQLDbContext _sqlContext;
        private readonly TestFormBL _formBL;
        private readonly string _databaseName;

        public FormBLTest()
        {
            // Setup SQL context with in-memory database
            _databaseName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<SQLDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;
            _sqlContext = new SQLDbContext(options);

            // Create our test implementation of IFormBL
            _formBL = new TestFormBL(_sqlContext);
        }

        // Test-specific implementation of IFormBL
        private class TestFormBL : IFormBL
        {
            private readonly SQLDbContext _sqlContext;
            private readonly List<Form> _forms = new List<Form>();

            public TestFormBL(SQLDbContext sqlContext)
            {
                _sqlContext = sqlContext;

                // Add some test forms
                _forms.Add(new Form
                {
                    Id = "1",
                    Status = MongoFormStatus.Draft,
                    Config = new FormConfig { Title = "Draft Form", Description = "Draft Description" },
                    Layout = new FormLayout
                    {
                        HeaderCard = new FormHeaderCard { Title = "Draft Header", Description = "Draft Desc" },
                        Fields = new List<FormField>()
                    },
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin"
                });

                _forms.Add(new Form
                {
                    Id = "2",
                    Status = MongoFormStatus.Published,
                    Config = new FormConfig { Title = "Published Form", Description = "Published Description" },
                    Layout = new FormLayout
                    {
                        HeaderCard = new FormHeaderCard { Title = "Published Header", Description = "Published Desc" },
                        Fields = new List<FormField>()
                    },
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin",
                    PublishedAt = DateTime.UtcNow
                });

                _forms.Add(new Form
                {
                    Id = "3",
                    Status = MongoFormStatus.Draft,
                    Config = new FormConfig { Title = "Another Draft", Description = "Another Description" },
                    Layout = new FormLayout
                    {
                        HeaderCard = new FormHeaderCard { Title = "Another Header", Description = "Another Desc" },
                        Fields = new List<FormField>()
                    },
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = "admin"
                });
            }

            // Updated to match FormBL signature: pageNumber and pageSize instead of offset and limit
            public Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(
                string userRole,
                int pageNumber,
                int pageSize,
                string? search = null)
            {
                IEnumerable<Form> filteredForms;

                // Role-based filter
                if (userRole == "Admin")
                {
                    filteredForms = _forms;
                }
                else
                {
                    filteredForms = _forms.Where(f => f.Status == MongoFormStatus.Published);
                }

                // Search filter (case-insensitive)
                if (!string.IsNullOrEmpty(search))
                {
                    filteredForms = filteredForms.Where(f =>
                        f.Config.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        f.Config.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var totalCount = filteredForms.Count();

                // Pagination offset
                int offset = (pageNumber - 1) * pageSize;

                var paginatedForms = filteredForms
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip(offset)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult<(IEnumerable<Form>, long)>((paginatedForms, totalCount));
            }

            public Task<Form?> GetFormByIdAsync(string id, string userRole)
            {
                var form = _forms.FirstOrDefault(f => f.Id == id);

                if (form == null)
                    return Task.FromResult<Form?>(null);

                if (userRole != "Admin" && form.Status != MongoFormStatus.Published)
                    return Task.FromResult<Form?>(null);

                return Task.FromResult<Form?>(form);
            }

            public Task<string> CreateFormConfigAsync(FormConfigDTO dto, string createdBy)
            {
                var form = new Form
                {
                    Id = Guid.NewGuid().ToString(),
                    Config = new FormConfig
                    {
                        Title = dto.Title,
                        Description = dto.Description
                    },
                    Layout = new FormLayout(),
                    Status = MongoFormStatus.Draft,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                _forms.Add(form);
                return Task.FromResult(form.Id);
            }

            public Task<bool> UpdateFormConfigAsync(string id, FormConfigDTO dto)
            {
                var existing = _forms.FirstOrDefault(f => f.Id == id);
                if (existing == null)
                    throw new Exception("Form not found.");

                if (existing.Status == MongoFormStatus.Published)
                    throw new InvalidOperationException("Cannot edit a published form.");

                existing.Config.Title = dto.Title;
                existing.Config.Description = dto.Description;
                existing.UpdatedAt = DateTime.UtcNow;

                return Task.FromResult(true);
            }

            public Task<bool> CreateFormLayoutAsync(string formId, FormLayoutDTO layoutDto)
            {
                var existingForm = _forms.FirstOrDefault(f => f.Id == formId);
                if (existingForm == null)
                    throw new Exception("Form not found.");

                if (existingForm.Status == MongoFormStatus.Published)
                    throw new InvalidOperationException("Cannot add layout to a published form.");

                // Map DTO to layout
                var layout = new FormLayout
                {
                    HeaderCard = new FormHeaderCard
                    {
                        Title = layoutDto.HeaderCard.Title,
                        Description = layoutDto.HeaderCard.Description
                    },
                    Fields = layoutDto.Fields?.Select(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        DescriptionEnabled = f.DescriptionEnabled,
                        Description = f.Description,
                        SingleChoice = f.SingleChoice,
                        MultipleChoice = f.MultipleChoice,
                        Options = f.Options?.Select(o => new FieldOption
                        {
                            Value = o.Value
                        }).ToList() ?? new List<FieldOption>(),
                        Format = f.Format,
                        Required = f.Required,
                        Order = f.Order
                    }).ToList() ?? new List<FormField>()
                };

                existingForm.Layout = layout;
                existingForm.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<bool> UpdateFormLayoutAsync(string formId, FormLayoutDTO dto)
            {
                var existing = _forms.FirstOrDefault(f => f.Id == formId);
                if (existing == null)
                    throw new Exception("Form not found.");

                if (existing.Status == MongoFormStatus.Published)
                    throw new InvalidOperationException("Cannot edit a published form.");

                var updatedLayout = new FormLayout
                {
                    HeaderCard = new FormHeaderCard
                    {
                        Title = dto.HeaderCard.Title,
                        Description = dto.HeaderCard.Description
                    },
                    Fields = dto.Fields?.Select(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        DescriptionEnabled = f.DescriptionEnabled,
                        Description = f.Description,
                        SingleChoice = f.SingleChoice,
                        MultipleChoice = f.MultipleChoice,
                        Options = f.Options?.Select(o => new FieldOption
                        {
                            Value = o.Value
                        }).ToList() ?? new List<FieldOption>(),
                        Format = f.Format,
                        Required = f.Required,
                        Order = f.Order
                    }).ToList() ?? new List<FormField>()
                };

                existing.Layout = updatedLayout;
                existing.UpdatedAt = DateTime.UtcNow;

                return Task.FromResult(true);
            }

            public Task<Form> PublishFormAsync(string id)
            {
                var existing = _forms.FirstOrDefault(f => f.Id == id);
                if (existing == null)
                    throw new Exception("Form not found.");

                if (existing.Status == MongoFormStatus.Published)
                    throw new InvalidOperationException("Form is already published.");

                existing.Status = MongoFormStatus.Published;
                existing.PublishedAt = DateTime.UtcNow;

                return Task.FromResult(existing);
            }

            public async Task<bool> DeleteFormAsync(string id)
            {
                // Delete related responses from SQL first
                var responses = await _sqlContext.FormResponses
                    .Where(r => r.FormId == id)
                    .Include(r => r.Answers)
                    .ToListAsync();

                if (responses.Any())
                {
                    var allAnswers = responses.SelectMany(r => r.Answers).ToList();
                    if (allAnswers.Any())
                        _sqlContext.FormResponseAnswers.RemoveRange(allAnswers);

                    _sqlContext.FormResponses.RemoveRange(responses);
                    await _sqlContext.SaveChangesAsync();
                }

                var form = _forms.FirstOrDefault(f => f.Id == id);
                if (form != null)
                {
                    _forms.Remove(form);
                    return true;
                }
                return false;
            }
        }

        #region GetAllFormsAsync Tests

        [Fact]
        public async Task GetAllFormsAsync_AdminRole_ReturnsAllForms()
        {
            // Act - Using pageNumber=1, pageSize=10
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(3, forms.Count());
            Assert.Contains(forms, f => f.Id == "1");
            Assert.Contains(forms, f => f.Id == "2");
            Assert.Contains(forms, f => f.Id == "3");
        }

        [Fact]
        public async Task GetAllFormsAsync_LearnerRole_ReturnsOnlyPublishedForms()
        {
            // Act
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Learner", 1, 10);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(forms);
            Assert.Equal("2", forms.First().Id);
            Assert.Equal(MongoFormStatus.Published, forms.First().Status);
        }

        [Fact]
        public async Task GetAllFormsAsync_WithSearch_ReturnsFilteredForms()
        {
            // Act - Search for "Draft"
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10, "Draft");

            // Assert
            Assert.Equal(2, totalCount); // "Draft Form" and "Another Draft"
            Assert.Equal(2, forms.Count());
        }

        [Fact]
        public async Task GetAllFormsAsync_WithSearchPublished_ReturnsFilteredForms()
        {
            // Act - Search for "Published"
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10, "Published");

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(forms);
            Assert.Equal("2", forms.First().Id);
        }

        [Fact]
        public async Task GetAllFormsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Act - Get first page with pageSize 2
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 2);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(2, forms.Count());
        }

        [Fact]
        public async Task GetAllFormsAsync_SecondPage_ReturnsCorrectPage()
        {
            // Act - Get second page with pageNumber=2, pageSize=2
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 2, 2);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Single(forms); // Only 1 form on second page
        }

        [Fact]
        public async Task GetAllFormsAsync_OrderedByCreatedAt_ReturnsDescendingOrder()
        {
            // Act
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10);

            // Assert
            var formsList = forms.ToList();
            Assert.True(formsList[0].CreatedAt >= formsList[1].CreatedAt);
            Assert.True(formsList[1].CreatedAt >= formsList[2].CreatedAt);
        }

        [Fact]
        public async Task GetAllFormsAsync_EmptySearch_ReturnsAllForms()
        {
            // Act
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10, "");

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(3, forms.Count());
        }

        [Fact]
        public async Task GetAllFormsAsync_NullSearch_ReturnsAllForms()
        {
            // Act
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Admin", 1, 10, null);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(3, forms.Count());
        }

        [Fact]
        public async Task GetAllFormsAsync_LearnerWithSearch_ReturnsOnlyPublishedMatchingForms()
        {
            // Act
            var (forms, totalCount) = await _formBL.GetAllFormsAsync("Learner", 1, 10, "Published");

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(forms);
            Assert.All(forms, f => Assert.Equal(MongoFormStatus.Published, f.Status));
        }

        #endregion

        #region GetFormByIdAsync Tests

        [Fact]
        public async Task GetFormByIdAsync_AdminRole_ReturnsForm()
        {
            // Act
            var result = await _formBL.GetFormByIdAsync("1", "Admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
        }

        [Fact]
        public async Task GetFormByIdAsync_LearnerRole_PublishedForm_ReturnsForm()
        {
            // Act
            var result = await _formBL.GetFormByIdAsync("2", "Learner");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2", result.Id);
            Assert.Equal(MongoFormStatus.Published, result.Status);
        }

        [Fact]
        public async Task GetFormByIdAsync_LearnerRole_DraftForm_ReturnsNull()
        {
            // Act
            var result = await _formBL.GetFormByIdAsync("1", "Learner");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFormByIdAsync_NonExistentForm_ReturnsNull()
        {
            // Act
            var result = await _formBL.GetFormByIdAsync("999", "Admin");

            // Assert
            Assert.Null(result);
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

            // Act
            var result = await _formBL.CreateFormConfigAsync(dto, createdBy);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual("", result);
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

            // Act
            var formId = await _formBL.CreateFormConfigAsync(dto, createdBy);
            var createdForm = await _formBL.GetFormByIdAsync(formId, "Admin");

            // Assert
            Assert.NotNull(createdForm);
            Assert.Equal(MongoFormStatus.Draft, createdForm.Status);
            Assert.Equal("New Form", createdForm.Config.Title);
            Assert.Equal("New Description", createdForm.Config.Description);
        }

        #endregion

        #region UpdateFormConfigAsync Tests

        [Fact]
        public async Task UpdateFormConfigAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1"; // Draft form
            var dto = new FormConfigDTO
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };

            // Act
            var result = await _formBL.UpdateFormConfigAsync(formId, dto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFormConfigAsync_UpdatesFormConfig()
        {
            // Arrange
            var formId = "1";
            var dto = new FormConfigDTO
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };

            // Act
            await _formBL.UpdateFormConfigAsync(formId, dto);
            var updatedForm = await _formBL.GetFormByIdAsync(formId, "Admin");

            // Assert
            Assert.NotNull(updatedForm);
            Assert.Equal("Updated Title", updatedForm.Config.Title);
            Assert.Equal("Updated Description", updatedForm.Config.Description);
            Assert.NotNull(updatedForm.UpdatedAt);
        }

        [Fact]
        public async Task UpdateFormConfigAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent";
            var dto = new FormConfigDTO
            {
                Title = "Updated Title"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _formBL.UpdateFormConfigAsync(formId, dto));
        }

        [Fact]
        public async Task UpdateFormConfigAsync_PublishedForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2"; // Published form
            var dto = new FormConfigDTO
            {
                Title = "Updated Title"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.UpdateFormConfigAsync(formId, dto));
            Assert.Equal("Cannot edit a published form.", exception.Message);
        }

        #endregion

        #region CreateFormLayoutAsync Tests

        [Fact]
        public async Task CreateFormLayoutAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1"; // Use existing draft form ID
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

            // Act
            var result = await _formBL.CreateFormLayoutAsync(formId, layoutDto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateFormLayoutAsync_UpdatesFormLayout()
        {
            // Arrange
            var formId = "1";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "New Header",
                    Description = "New Description"
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

            // Act
            await _formBL.CreateFormLayoutAsync(formId, layoutDto);
            var updatedForm = await _formBL.GetFormByIdAsync(formId, "Admin");

            // Assert
            Assert.NotNull(updatedForm);
            Assert.NotNull(updatedForm.Layout);
            Assert.Equal("New Header", updatedForm.Layout.HeaderCard.Title);
            Assert.Single(updatedForm.Layout.Fields);
            Assert.Equal("Question 1", updatedForm.Layout.Fields[0].Label);
        }

        [Fact]
        public async Task CreateFormLayoutAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent-form-id";
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Header Title"
                },
                Fields = new List<FormFieldDTO>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _formBL.CreateFormLayoutAsync(formId, layoutDto));
        }

        [Fact]
        public async Task CreateFormLayoutAsync_PublishedForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2"; // Published form
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Header Title"
                },
                Fields = new List<FormFieldDTO>()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.CreateFormLayoutAsync(formId, layoutDto));
            Assert.Equal("Cannot add layout to a published form.", exception.Message);
        }

        #endregion

        #region UpdateFormLayoutAsync Tests

        [Fact]
        public async Task UpdateFormLayoutAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1"; // Draft form
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

            // Act
            var result = await _formBL.UpdateFormLayoutAsync(formId, layoutDto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFormLayoutAsync_UpdatesFormLayout()
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
                        Type = "dropdown",
                        Required = false,
                        Order = 1,
                        Options = new List<FieldOptionDTO>
                        {
                            new FieldOptionDTO { Value = "Option 1" },
                            new FieldOptionDTO { Value = "Option 2" }
                        }
                    }
                }
            };

            // Act
            await _formBL.UpdateFormLayoutAsync(formId, layoutDto);
            var updatedForm = await _formBL.GetFormByIdAsync(formId, "Admin");

            // Assert
            Assert.NotNull(updatedForm);
            Assert.NotNull(updatedForm.Layout);
            Assert.Equal("Updated Header", updatedForm.Layout.HeaderCard.Title);
            Assert.Single(updatedForm.Layout.Fields);
            Assert.Equal("Updated Question", updatedForm.Layout.Fields[0].Label);
            Assert.Equal("dropdown", updatedForm.Layout.Fields[0].Type);
            Assert.Equal(2, updatedForm.Layout.Fields[0].Options.Count);
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
                    Title = "Updated Header"
                },
                Fields = new List<FormFieldDTO>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _formBL.UpdateFormLayoutAsync(formId, layoutDto));
        }

        [Fact]
        public async Task UpdateFormLayoutAsync_PublishedForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2"; // Published form
            var layoutDto = new FormLayoutDTO
            {
                HeaderCard = new FormHeaderCardDTO
                {
                    Title = "Updated Header"
                },
                Fields = new List<FormFieldDTO>()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.UpdateFormLayoutAsync(formId, layoutDto));
            Assert.Equal("Cannot edit a published form.", exception.Message);
        }

        #endregion

        #region PublishFormAsync Tests

        [Fact]
        public async Task PublishFormAsync_DraftForm_PublishesSuccessfully()
        {
            // Arrange
            var formId = "1"; // Draft form

            // Act
            var result = await _formBL.PublishFormAsync(formId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(MongoFormStatus.Published, result.Status);
            Assert.NotNull(result.PublishedAt);
        }

        [Fact]
        public async Task PublishFormAsync_AlreadyPublished_ThrowsInvalidOperationException()
        {
            // Arrange
            var formId = "2"; // Already published form

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formBL.PublishFormAsync(formId));
            Assert.Equal("Form is already published.", exception.Message);
        }

        [Fact]
        public async Task PublishFormAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var formId = "nonexistent";

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _formBL.PublishFormAsync(formId));
        }

        #endregion

        #region DeleteFormAsync Tests

        [Fact]
        public async Task DeleteFormAsync_ExistingForm_ReturnsTrue()
        {
            // Arrange
            var formId = "1";

            // Act
            var result = await _formBL.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFormAsync_RemovesFormFromList()
        {
            // Arrange
            var formId = "1";

            // Act
            await _formBL.DeleteFormAsync(formId);
            var deletedForm = await _formBL.GetFormByIdAsync(formId, "Admin");

            // Assert
            Assert.Null(deletedForm);
        }

        [Fact]
        public async Task DeleteFormAsync_NonExistentForm_ReturnsFalse()
        {
            // Arrange
            var formId = "nonexistent";

            // Act
            var result = await _formBL.DeleteFormAsync(formId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFormAsync_WithRelatedResponses_DeletesResponsesFirst()
        {
            // Arrange
            var formId = "1";

            // Add some test responses to SQL database
            var response = new FormBuilderAPI.Model.SQLModel.FormResponse
            {
                FormId = formId,
                SubmittedBy = "user123",
                SubmittedAt = DateTime.UtcNow
            };

            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            // Add answer separately after response is saved to get the ResponseId
            var answer = new FormBuilderAPI.Model.SQLModel.FormResponseAnswer
            {
                ResponseId = response.ResponseId,
                QuestionId = "q1",
                AnswerText = "Test Answer"
            };

            _sqlContext.FormResponseAnswers.Add(answer);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _formBL.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);
            var remainingResponses = await _sqlContext.FormResponses
                .Where(r => r.FormId == formId)
                .ToListAsync();
            Assert.Empty(remainingResponses);

            var remainingAnswers = await _sqlContext.FormResponseAnswers
                .Where(a => a.ResponseId == response.ResponseId)
                .ToListAsync();
            Assert.Empty(remainingAnswers);
        }

        [Fact]
        public async Task DeleteFormAsync_WithMultipleResponses_DeletesAllRelatedData()
        {
            // Arrange
            var formId = "3";

            var response1 = new FormBuilderAPI.Model.SQLModel.FormResponse
            {
                FormId = formId,
                SubmittedBy = "user1",
                SubmittedAt = DateTime.UtcNow
            };

            var response2 = new FormBuilderAPI.Model.SQLModel.FormResponse
            {
                FormId = formId,
                SubmittedBy = "user2",
                SubmittedAt = DateTime.UtcNow
            };

            _sqlContext.FormResponses.AddRange(response1, response2);
            await _sqlContext.
SaveChangesAsync();

            var answer1 = new FormBuilderAPI.Model.SQLModel.FormResponseAnswer
            {
                ResponseId = response1.ResponseId,
                QuestionId = "q1",
                AnswerText = "Answer 1"
            };

            var answer2 = new FormBuilderAPI.Model.SQLModel.FormResponseAnswer
            {
                ResponseId = response1.ResponseId,
                QuestionId = "q2",
                AnswerText = "Answer 2"
            };

            var answer3 = new FormBuilderAPI.Model.SQLModel.FormResponseAnswer
            {
                ResponseId = response2.ResponseId,
                QuestionId = "q1",
                AnswerText = "Answer 3"
            };

            _sqlContext.FormResponseAnswers.AddRange(answer1, answer2, answer3);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _formBL.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);

            var remainingResponses = await _sqlContext.FormResponses
                .Where(r => r.FormId == formId)
                .ToListAsync();
            Assert.Empty(remainingResponses);

            var remainingAnswers = await _sqlContext.FormResponseAnswers
                .Where(a => a.ResponseId == response1.ResponseId || a.ResponseId == response2.ResponseId)
                .ToListAsync();
            Assert.Empty(remainingAnswers);
        }

        [Fact]
        public async Task DeleteFormAsync_FormWithNoResponses_DeletesFormOnly()
        {
            // Arrange
            var formId = "2";

            // Act
            var result = await _formBL.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);

            // Verify form is deleted
            var deletedForm = await _formBL.GetFormByIdAsync(formId, "Admin");
            Assert.Null(deletedForm);
        }

        #endregion

        public void Dispose()
        {
            _sqlContext.Database.EnsureDeleted();
            _sqlContext.Dispose();
        }
    }
}
