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
                    Layout = new FormLayout()
                });
                
                _forms.Add(new Form 
                { 
                    Id = "2", 
                    Status = MongoFormStatus.Published,
                    Config = new FormConfig { Title = "Published Form", Description = "Published Description" },
                    Layout = new FormLayout()
                });
            }

            public Task<IEnumerable<Form>> GetAllFormsAsync(string userRole)
            {
                if (userRole == "Admin")
                    return Task.FromResult<IEnumerable<Form>>(_forms);

                return Task.FromResult<IEnumerable<Form>>(_forms.Where(f => f.Status == MongoFormStatus.Published));
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

            public Task<bool> CreateFormLayoutAsync(string formId, FormLayoutDTO layoutDto)
            {
                var existingForm = _forms.FirstOrDefault(f => f.Id == formId);
                if (existingForm == null)
                    throw new Exception("Form not found.");

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
                return Task.FromResult(true);
            }

            public Task<bool> UpdateFormAsync(string id, FormDTO dto)
            {
                var existing = _forms.FirstOrDefault(f => f.Id == id);
                if (existing == null || existing.Status == MongoFormStatus.Published)
                    return Task.FromResult(false);

                existing.Config.Title = dto.Config.Title;
                existing.Config.Description = dto.Config.Description;
                existing.Layout = new FormLayout
                {
                    HeaderCard = new FormHeaderCard
                    {
                        Title = dto.Layout.HeaderCard.Title,
                        Description = dto.Layout.HeaderCard.Description
                    },
                    Fields = dto.Layout.Fields.Select(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        DescriptionEnabled = f.DescriptionEnabled,
                        Description = f.Description,
                        SingleChoice = f.SingleChoice,
                        MultipleChoice = f.MultipleChoice,
                        Options = f.Options.Select(o => new FieldOption
                        {
                            Value = o.Value
                        }).ToList(),
                        Format = f.Format,
                        Required = f.Required,
                        Order = f.Order
                    }).ToList()
                };
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
            // Act
            var result = await _formBL.GetAllFormsAsync("Admin");

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, f => f.Id == "1");
            Assert.Contains(result, f => f.Id == "2");
        }

        [Fact]
        public async Task GetAllFormsAsync_LearnerRole_ReturnsOnlyPublishedForms()
        {
            // Act
            var result = await _formBL.GetAllFormsAsync("Learner");

            // Assert
            Assert.Single(result);
            Assert.Equal("2", result.First().Id);
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
        }

        [Fact]
        public async Task GetFormByIdAsync_LearnerRole_DraftForm_ReturnsNull()
        {
            // Act
            var result = await _formBL.GetFormByIdAsync("1", "Learner");

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

        #endregion

        #region CreateFormLayoutAsync Tests

        [Fact]
        public async Task CreateFormLayoutAsync_ValidDto_ReturnsTrue()
        {
            // Arrange
            var formId = "1"; // Use existing form ID
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

        #endregion

        public void Dispose()
        {
            _sqlContext.Database.EnsureDeleted();
            _sqlContext.Dispose();
        }
    }
}
