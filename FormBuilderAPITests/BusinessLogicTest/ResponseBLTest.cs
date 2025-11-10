using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.Repository;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Model.MongoModel;
using FormBuilderAPI.DTOs;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FormBuilderAPITests.BusinessLogicTest
{
    public class ResponseBLTest
    {
        private readonly Mock<IResponseRepository> _mockResponseRepository;
        private readonly Mock<IFormBL> _mockFormBL;
        private readonly IResponseBL _responseBL;

        public ResponseBLTest()
        {
            _mockResponseRepository = new Mock<IResponseRepository>();
            _mockFormBL = new Mock<IFormBL>();
            _responseBL = new ResponseBL(_mockResponseRepository.Object, _mockFormBL.Object);
        }

        #region SubmitResponseAsync Tests

        [Fact]
        public async Task SubmitResponseAsync_NullDto_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _responseBL.SubmitResponseAsync(null!));
        }

        [Fact]
        public async Task SubmitResponseAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            var responseDto = new ResponseDTO
            {
                FormId = "non-existent-form",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>()
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("non-existent-form", "Learner"))
                .ReturnsAsync((Form?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _responseBL.SubmitResponseAsync(responseDto));
            Assert.Contains("Form not found or access denied", exception.Message);
        }

        [Fact]
        public async Task SubmitResponseAsync_FormWithNullLayout_ThrowsException()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-123",
                Config = new FormConfig { Title = "Test Form" },
                Layout = null!
            };

            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>()
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _responseBL.SubmitResponseAsync(responseDto));
            Assert.Contains("Form not found or access denied", exception.Message);
        }

        [Fact]
        public async Task SubmitResponseAsync_ValidTextResponse_ReturnsResponseId()
        {
            // Arrange
            var form = CreateTestForm();
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q1",
                        AnswerText = "Answer 1"
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseAsync(It.Is<FormResponse>(fr =>
                fr.FormId == "form-123" &&
                fr.SubmittedBy == "user1" &&
                fr.Answers.Count == 1
            )), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_WithDropdownSingleChoice_SavesOptionId()
        {
            // Arrange
            var form = CreateTestFormWithDropdown(false);
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-dropdown",
                        AnswerText = "Option 1"
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseAsync(It.Is<FormResponse>(fr =>
                fr.Answers.Any(a => a.QuestionId == "q-dropdown" && a.AnswerText.Contains("opt1"))
            )), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_WithDropdownMultipleChoice_SavesMultipleOptionIds()
        {
            // Arrange
            var form = CreateTestFormWithDropdown(true);
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-dropdown",
                        AnswerText = "Option 1,Option 2"
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseAsync(It.Is<FormResponse>(fr =>
                fr.Answers.Any(a => a.QuestionId == "q-dropdown" && 
                    a.AnswerText.Contains("opt1") && a.AnswerText.Contains("opt2"))
            )), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_WithFileUpload_SavesFileCorrectly()
        {
            // Arrange
            var form = CreateTestFormWithFileUpload();
            var fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test file content"));
            
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-file",
                        File = new ResponseFileUploadDTO
                        {
                            QuestionId = "q-file",
                            FileName = "test.pdf",
                            FileType = "application/pdf",
                            FileMaxSize = 1024,
                            Base64Content = fileContent
                        }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            _mockResponseRepository
                .Setup(r => r.InsertResponseFileAsync(It.IsAny<ResponseFile>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseFileAsync(It.Is<ResponseFile>(rf =>
                rf.ResponseId == 1 &&
                rf.QuestionId == "q-file" &&
                rf.FileName == "test.pdf" &&
                rf.FileType == "application/pdf"
            )), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_FileExceedsMaxSize_ThrowsException()
        {
            // Arrange
            var form = CreateTestFormWithFileUpload();
            var largeFileContent = Convert.ToBase64String(new byte[6 * 1024 * 1024]); // 6 MB
            
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-file",
                        File = new ResponseFileUploadDTO
                        {
                            QuestionId = "q-file",
                            FileName = "large.pdf",
                            FileType = "application/pdf",
                            FileMaxSize = 6 * 1024 * 1024,
                            Base64Content = largeFileContent
                        }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _responseBL.SubmitResponseAsync(responseDto));
            Assert.Contains("exceeds the 5 MB limit", exception.Message);
        }

        [Fact]
        public async Task SubmitResponseAsync_InvalidFileType_ThrowsException()
        {
            // Arrange
            var form = CreateTestFormWithFileUpload();
            var fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test content"));
            
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-file",
                        File = new ResponseFileUploadDTO
                        {
                            QuestionId = "q-file",
                            FileName = "test.exe",
                            FileType = "application/exe",
                            FileMaxSize = 1024,
                            Base64Content = fileContent
                        }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _responseBL.SubmitResponseAsync(responseDto));
            Assert.Contains("has invalid type", exception.Message);
        }

        [Fact]
        public async Task SubmitResponseAsync_MixedAnswerTypes_ProcessesCorrectly()
        {
            // Arrange
            var form = CreateTestFormWithMixedFields();
            var fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test file"));
            
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO { QuestionId = "q-text", AnswerText = "Text answer" },
                    new ResponseAnswerDTO { QuestionId = "q-dropdown", AnswerText = "Option 1" },
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-file",
                        File = new ResponseFileUploadDTO
                        {
                            QuestionId = "q-file",
                            FileName = "test.pdf",
                            FileType = "application/pdf",
                            FileMaxSize = 1024,
                            Base64Content = fileContent
                        }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            _mockResponseRepository
                .Setup(r => r.InsertResponseFileAsync(It.IsAny<ResponseFile>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseAsync(It.Is<FormResponse>(fr =>
                fr.Answers.Count == 2 // Text and dropdown only
            )), Times.Once);
            _mockResponseRepository.Verify(r => r.InsertResponseFileAsync(It.IsAny<ResponseFile>()), Times.Once);
        }

        #endregion

        #region GetResponsesForUserAsync Tests

        [Fact]
        public async Task GetResponsesForUserAsync_NoResponses_ReturnsEmptyList()
        {
            // Arrange
            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAndUserIdAsync("form-123", "user1"))
                .ReturnsAsync(new List<FormResponse>());

            // Act
            var result = await _responseBL.GetResponsesForUserAsync("form-123", "user1");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetResponsesForUserAsync_WithResponses_ReturnsResponseDetailDTOs()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer
                        {
                            AnswerId = 1,
                            QuestionId = "q1",
                            AnswerText = "Answer 1"
                        }
                    }
                }
            };

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAndUserIdAsync("form-123", "user1"))
                .ReturnsAsync(responses);

            // Act
            var result = await _responseBL.GetResponsesForUserAsync("form-123", "user1");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].ResponseId);
            Assert.Equal("form-123", result[0].FormId);
            Assert.Equal("user1", result[0].SubmittedBy);
            Assert.Single(result[0].Answers);
        }

        [Fact]
        public async Task GetResponsesForUserAsync_MultipleResponses_ReturnsAllResponses()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                },
                new FormResponse
                {
                    ResponseId = 2,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAndUserIdAsync("form-123", "user1"))
                .ReturnsAsync(responses);

            // Act
            var result = await _responseBL.GetResponsesForUserAsync("form-123", "user1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetResponsesForFormAsync Tests

        [Fact]
        public async Task GetResponsesForFormAsync_NoResponses_ReturnsEmptyPagedResult()
        {
            // Arrange
            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(new List<FormResponse>());

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var items = GetPropertyValue<List<object>>(result, "Items");
            Assert.Equal(0, totalCount);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetResponsesForFormAsync_WithResponses_ReturnsPagedResult()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            var users = new Dictionary<string, string> { { "1", "User One" } };

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(users);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var pageNumber = GetPropertyValue<int>(result, "PageNumber");
            var items = GetPropertyValue<List<object>>(result, "Items");
            
            Assert.Equal(1, totalCount);
            Assert.Equal(1, pageNumber);
            Assert.Single(items);
        }

        [Fact]
        public async Task GetResponsesForFormAsync_WithSearch_FiltersResults()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                },
                new FormResponse
                {
                    ResponseId = 2,
                    FormId = "form-123",
                    SubmittedBy = "2",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            var users = new Dictionary<string, string>
            {
                { "1", "John Doe" },
                { "2", "Jane Smith" }
            };

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(users);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123", "John");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var items = GetPropertyValue<List<object>>(result, "Items");
            
            Assert.Equal(1, totalCount);
            Assert.Single(items);
        }

        [Fact]
        public async Task GetResponsesForFormAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var responses = Enumerable.Range(1, 10).Select(i => new FormResponse
            {
                ResponseId = i,
                FormId = "form-123",
                SubmittedBy = i.ToString(),
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>()
            }).ToList();

            var users = responses.ToDictionary(r => r.SubmittedBy, r => $"User {r.SubmittedBy}");

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(users);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123", null, 2, 3);

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var pageNumber = GetPropertyValue<int>(result, "PageNumber");
            var pageSize = GetPropertyValue<int>(result, "PageSize");
            var items = GetPropertyValue<List<object>>(result, "Items");
            
            Assert.Equal(10, totalCount);
            Assert.Equal(2, pageNumber);
            Assert.Equal(3, pageSize);
            Assert.Equal(3, items.Count);
        }

        [Fact]
        public async Task GetResponsesForFormAsync_WithFiles_IncludesFileData()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            var users = new Dictionary<string, string> { { "1", "User One" } };
            var files = new List<ResponseFile>
            {
                new ResponseFile
                {
                    FileId = 1,
                    ResponseId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileType = "application/pdf",
                    FileMaxSize = 1024,
                    Base64Content = "base64content",
                    UploadedAt = DateTime.UtcNow
                }
            };

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(users);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(1))
                .ReturnsAsync(files);

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123");

            // Assert
            Assert.NotNull(result);
            var items = GetPropertyValue<List<object>>(result, "Items");
            Assert.Single(items);
        }

        #endregion

        #region GetAllResponsesByUserAsync Tests

        [Fact]
        public async Task GetAllResponsesByUserAsync_NoResponses_ReturnsEmptyResult()
        {
            // Arrange
            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(new List<FormResponse>());

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            Assert.Equal(0, totalCount);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_WithResponses_ReturnsPagedResult()
        {
            // Arrange
            var form = CreateTestForm();
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { AnswerId = 1, QuestionId = "q1", AnswerText = "Answer 1" }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Admin"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            
            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Equal("Test Form", items[0].FormTitle);
            Assert.Equal("Test Description", items[0].FormDescription);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_WithSearch_FiltersResults()
        {
            // Arrange
            var form1 = new Form
            {
                Id = "form-1",
                Config = new FormConfig { Title = "Customer Survey", Description = "Survey about customers" },
                Layout = new FormLayout { Fields = new List<FormField>() }
            };

            var form2 = new Form
            {
                Id = "form-2",
                Config = new FormConfig { Title = "Employee Feedback", Description = "Feedback form" },
                Layout = new FormLayout { Fields = new List<FormField>() }
            };

            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-1",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                },
                new FormResponse
                {
                    ResponseId = 2,
                    FormId = "form-2",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-1", "Admin"))
                .ReturnsAsync(form1);

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-2", "Admin"))
                .ReturnsAsync(form2);

            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1", "Customer");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            
            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Contains("Customer", items[0].FormTitle);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var responses = Enumerable.Range(1, 10).Select(i => new FormResponse
            {
                ResponseId = i,
                FormId = $"form-{i}",
                SubmittedBy = "user1",
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>()
            }).ToList();

            foreach (var response in responses)
            {
                var form = new Form
                {
                    Id = response.FormId,
                    Config = new FormConfig { Title = $"Form {response.ResponseId}", Description = "Description" },
                    Layout = new FormLayout { Fields = new List<FormField>() }
                };

                _mockFormBL
                    .Setup(m => m.GetFormByIdAsync(response.FormId, "Admin"))
                    .ReturnsAsync(form);
            }

            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1", null, 2, 3);

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var pageNumber = GetPropertyValue<int>(result, "PageNumber");
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            
            Assert.Equal(10, totalCount);
            Assert.Equal(2, pageNumber);
            Assert.Equal(3, items.Count);
        }

                [Fact]
        public async Task GetAllResponsesByUserAsync_FormNotFound_UsesUnknownForm()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "non-existent-form",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("non-existent-form", "Admin"))
                .ThrowsAsync(new Exception("Form not found"));

            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1");

            // Assert
            Assert.NotNull(result);
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            Assert.Single(items);
            Assert.Equal("Unknown Form", items[0].FormTitle);
            Assert.Equal("", items[0].FormDescription);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_WithFiles_IncludesFileData()
        {
            // Arrange
            var form = CreateTestForm();
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            var files = new List<ResponseFile>
            {
                new ResponseFile
                {
                    FileId = 1,
                    ResponseId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileType = "application/pdf",
                    FileMaxSize = 1024,
                    Base64Content = "base64content",
                    UploadedAt = DateTime.UtcNow
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Admin"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(1))
                .ReturnsAsync(files);

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1");

            // Assert
            Assert.NotNull(result);
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            Assert.Single(items);
            Assert.Single(items[0].Files);
            Assert.Equal("test.pdf", items[0].Files[0].FileName);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_SearchByAnswerText_FiltersCorrectly()
        {
            // Arrange
            var form = CreateTestForm();
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { AnswerId = 1, QuestionId = "q1", AnswerText = "Specific Answer" }
                    }
                },
                new FormResponse
                {
                    ResponseId = 2,
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { AnswerId = 2, QuestionId = "q1", AnswerText = "Different Answer" }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Admin"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.GetResponsesByUserIdAsync("user1"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetAllResponsesByUserAsync("user1", "Specific");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            var items = GetPropertyValue<List<ResponseDetailDTO>>(result, "Items");
            
            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Contains("Specific", items[0].Answers[0].AnswerText);
        }

        #endregion

        #region GetFileByResponseIdAndFileIdAsync Tests

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_FileExists_ReturnsFileDTO()
        {
            // Arrange
            var responseFile = new ResponseFile
            {
                FileId = 1,
                ResponseId = 1,
                QuestionId = "q1",
                FileName = "test.pdf",
                FileType = "application/pdf",
                FileMaxSize = 1024,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test content")),
                UploadedAt = DateTime.UtcNow
            };

            _mockResponseRepository
                .Setup(r => r.GetFileByResponseIdAndFileIdAsync(1, 1))
                .ReturnsAsync(responseFile);

            // Act
            var result = await _responseBL.GetFileByResponseIdAndFileIdAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ResponseId);
            Assert.Equal("q1", result.QuestionId);
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal("application/pdf", result.FileType);
            Assert.Equal(1024, result.FileMaxSize);
            Assert.NotEmpty(result.Base64Content);
        }

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_FileNotFound_ReturnsNull()
        {
            // Arrange
            _mockResponseRepository
                .Setup(r => r.GetFileByResponseIdAndFileIdAsync(999, 999))
                .ReturnsAsync((ResponseFile?)null);

            // Act
            var result = await _responseBL.GetFileByResponseIdAndFileIdAsync(999, 999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_DifferentFileTypes_ReturnsCorrectly()
        {
            // Arrange
            var fileTypes = new[]
            {
                ("image.jpg", "image/jpeg"),
                ("document.pdf", "application/pdf"),
                ("sheet.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
                ("picture.png", "image/png")
            };

            foreach (var (fileName, fileType) in fileTypes)
            {
                var responseFile = new ResponseFile
                {
                    FileId = 1,
                    ResponseId = 1,
                    QuestionId = "q1",
                    FileName = fileName,
                    FileType = fileType,
                    FileMaxSize = 2048,
                    Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Content")),
                    UploadedAt = DateTime.UtcNow
                };

                _mockResponseRepository
                    .Setup(r => r.GetFileByResponseIdAndFileIdAsync(1, 1))
                    .ReturnsAsync(responseFile);

                // Act
                var result = await _responseBL.GetFileByResponseIdAndFileIdAsync(1, 1);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(fileName, result.FileName);
                Assert.Equal(fileType, result.FileType);
            }
        }

        #endregion

        #region Edge Cases and Validation Tests

        [Fact]
        public async Task SubmitResponseAsync_EmptyAnswersList_ProcessesSuccessfully()
        {
            // Arrange
            var form = CreateTestForm();
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>()
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SubmitResponseAsync_NullAnswersList_ProcessesSuccessfully()
        {
            // Arrange
            var form = CreateTestForm();
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = null!
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SubmitResponseAsync_NullSubmittedBy_UsesEmptyString()
        {
            // Arrange
            var form = CreateTestForm();
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = null,
                Answers = new List<ResponseAnswerDTO>()
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseAsync(It.Is<FormResponse>(fr =>
                fr.SubmittedBy == string.Empty
            )), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_DropdownWithNoMatchingOptions_SavesEmptyArray()
        {
            // Arrange
            var form = CreateTestFormWithDropdown(false);
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-dropdown",
                        AnswerText = "Non-existent Option"
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SubmitResponseAsync_MultipleFilesAllowed_SavesAllFiles()
        {
            // Arrange
            var form = CreateTestFormWithFileUpload();
            var fileContent1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("File 1"));
            var fileContent2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("File 2"));
            
            var responseDto = new ResponseDTO
            {
                FormId = "form-123",
                SubmittedBy = "user1",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-file",
                        File = new ResponseFileUploadDTO
                        {
                            QuestionId = "q-file",
                            FileName = "test1.pdf",
                            FileType = "application/pdf",
                            FileMaxSize = 1024,
                            Base64Content = fileContent1
                        }
                    },
                    new ResponseAnswerDTO
                    {
                        QuestionId = "q-file",
                        File = new ResponseFileUploadDTO
                        {
                            QuestionId = "q-file",
                            FileName = "test2.pdf",
                            FileType = "application/pdf",
                            FileMaxSize = 2048,
                            Base64Content = fileContent2
                        }
                    }
                }
            };

            _mockFormBL
                .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                .ReturnsAsync(form);

            _mockResponseRepository
                .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                .ReturnsAsync(1);

            _mockResponseRepository
                .Setup(r => r.InsertResponseFileAsync(It.IsAny<ResponseFile>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.Equal(1, result);
            _mockResponseRepository.Verify(r => r.InsertResponseFileAsync(It.IsAny<ResponseFile>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SubmitResponseAsync_AllowedFileExtensions_AcceptsValidFiles()
        {
            // Arrange
            var form = CreateTestFormWithFileUpload();
            var validExtensions = new[] { "test.jpg", "test.jpeg", "test.png", "test.pdf", "test.docx" };

            foreach (var fileName in validExtensions)
            {
                var fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("Content"));
                var responseDto = new ResponseDTO
                {
                    FormId = "form-123",
                    SubmittedBy = "user1",
                    Answers = new List<ResponseAnswerDTO>
                    {
                        new ResponseAnswerDTO
                        {
                            QuestionId = "q-file",
                            File = new ResponseFileUploadDTO
                            {
                                QuestionId = "q-file",
                                FileName = fileName,
                                FileType = "application/octet-stream",
                                FileMaxSize = 1024,
                                Base64Content = fileContent
                            }
                        }
                    }
                };

                _mockFormBL
                    .Setup(m => m.GetFormByIdAsync("form-123", "Learner"))
                    .ReturnsAsync(form);

                _mockResponseRepository
                    .Setup(r => r.InsertResponseAsync(It.IsAny<FormResponse>()))
                    .ReturnsAsync(1);

                _mockResponseRepository
                    .Setup(r => r.InsertResponseFileAsync(It.IsAny<ResponseFile>()))
                    .Returns(Task.CompletedTask);

                // Act
                var result = await _responseBL.SubmitResponseAsync(responseDto);

                // Assert
                Assert.Equal(1, result);
            }
        }

        [Fact]
        public async Task GetResponsesForFormAsync_SearchByUserId_FiltersCorrectly()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "user123",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                },
                new FormResponse
                {
                    ResponseId = 2,
                    FormId = "form-123",
                    SubmittedBy = "user456",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            var users = new Dictionary<string, string>
            {
                { "user123", "John Doe" },
                { "user456", "Jane Smith" }
            };

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(users);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123", "user123");

            // Assert
            Assert.NotNull(result);
            var totalCount = GetPropertyValue<int>(result, "TotalCount");
            Assert.Equal(1, totalCount);
        }

        [Fact]
        public async Task GetResponsesForFormAsync_UnknownUser_ShowsUnknownInUserName()
        {
            // Arrange
            var responses = new List<FormResponse>
            {
                new FormResponse
                {
                    ResponseId = 1,
                    FormId = "form-123",
                    SubmittedBy = "unknown-user",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>()
                }
            };

            var users = new Dictionary<string, string>(); // Empty - user not found

            _mockResponseRepository
                .Setup(r => r.GetResponsesByFormIdAsync("form-123"))
                .ReturnsAsync(responses);

            _mockResponseRepository
                .Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(users);

            _mockResponseRepository
                .Setup(r => r.GetFilesByResponseIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ResponseFile>());

            // Act
            var result = await _responseBL.GetResponsesForFormAsync("form-123");

            // Assert
            Assert.NotNull(result);
            var items = GetPropertyValue<List<object>>(result, "Items");
            Assert.Single(items);
        }

        #endregion

                #region Helper Methods

        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{obj.GetType().Name}'");
            
            var value = property.GetValue(obj);
            
            // Handle List<ResponseDetailDTO> conversion
            if (typeof(T) == typeof(List<ResponseDetailDTO>) && value != null)
            {
                var sourceList = value as System.Collections.IEnumerable;
                if (sourceList != null)
                {
                    var resultList = new List<ResponseDetailDTO>();
                    foreach (var item in sourceList)
                    {
                        if (item is ResponseDetailDTO dto)
                        {
                            resultList.Add(dto);
                        }
                    }
                    return (T)(object)resultList;
                }
            }
            
            return (T)value!;
        }

        private Form CreateTestForm()
        {
            return new Form
            {
                Id = "form-123",
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Layout = new FormLayout
                {
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            QuestionId = "q1",
                            Label = "Question 1",
                            Type = "text",
                            Required = true
                        }
                    }
                }
            };
        }

        private Form CreateTestFormWithDropdown(bool multipleChoice)
        {
            return new Form
            {
                Id = "form-123",
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Layout = new FormLayout
                {
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            QuestionId = "q-dropdown",
                            Label = "Dropdown Question",
                            Type = "drop-down",
                            MultipleChoice = multipleChoice,
                            Options = new List<FieldOption>
                            {
                                new FieldOption { OptionId = "opt1", Value = "Option 1" },
                                new FieldOption { OptionId = "opt2", Value = "Option 2" },
                                new FieldOption { OptionId = "opt3", Value = "Option 3" }
                            }
                        }
                    }
                }
            };
        }

        private Form CreateTestFormWithFileUpload()
        {
            return new Form
            {
                Id = "form-123",
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Layout = new FormLayout
                {
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            QuestionId = "q-file",
                            Label = "File Upload",
                            Type = "file-upload",
                            Required = false
                        }
                    }
                }
            };
        }

        private Form CreateTestFormWithMixedFields()
        {
            return new Form
            {
                Id = "form-123",
                Config = new FormConfig { Title = "Test Form", Description = "Test Description" },
                Layout = new FormLayout
                {
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            QuestionId = "q-text",
                            Label = "Text Question",
                            Type = "text"
                        },
                        new FormField
                        {
                            QuestionId = "q-dropdown",
                            Label = "Dropdown Question",
                            Type = "drop-down",
                            Options = new List<FieldOption>
                            {
                                new FieldOption { OptionId = "opt1", Value = "Option 1" },
                                new FieldOption { OptionId = "opt2", Value = "Option 2" }
                            }
                        },
                        new FormField
                        {
                            QuestionId = "q-file",
                            Label = "File Upload",
                            Type = "file-upload"
                        }
                    }
                }
            };
        }

        #endregion
    }
}
