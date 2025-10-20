using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Model.MongoModel;
using FormBuilderAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Text;
using Moq;

namespace FormBuilderAPITests.BusinessLogicTest
{
    public class ResponseBLTest : IDisposable
    {
        private readonly SQLDbContext _context;
        private readonly IResponseBL _responseBL;
        private readonly Mock<IFormBL> _mockFormBL;
        private readonly string _databaseName;

        public ResponseBLTest()
        {
            _databaseName = Guid.NewGuid().ToString(); // Use a unique database name for each test run
            var options = new DbContextOptionsBuilder<SQLDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;
                
            _context = new SQLDbContext(options);
            
            // Create a mock for IFormBL
            _mockFormBL = new Mock<IFormBL>();
            
            // Set up the mock FormBL to return a form when GetFormByIdAsync is called
            SetupMockFormBL();
            
            // Initialize ResponseBL with the context and mock FormBL
            _responseBL = new ResponseBL(_context, _mockFormBL.Object);
        }

        private void SetupMockFormBL()
        {
            // Create a form that matches our test data
            var form = new Form
            {
                Id = "11111111-1111-1111-1111-111111111111",
                Config = new FormConfig
                {
                    Title = "Test Form",
                    Description = "Test Form Description"
                },
                Layout = new FormLayout
                {
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            QuestionId = "22222222-2222-2222-2222-222222222222",
                            Label = "Single Choice Question",
                            Type = "single-choice",
                            SingleChoice = true,
                            Options = new List<FieldOption>
                            {
                                new FieldOption { OptionId = "33333333-3333-3333-3333-333333333333", Value = "Option 1" },
                                new FieldOption { OptionId = "44444444-4444-4444-4444-444444444444", Value = "Option 2" }
                            }
                        },
                        new FormField
                        {
                            QuestionId = "55555555-5555-5555-5555-555555555555",
                            Label = "Multiple Choice Question",
                            Type = "multiple-choice",
                            MultipleChoice = true,
                            Options = new List<FieldOption>
                            {
                                new FieldOption { OptionId = "66666666-6666-6666-6666-666666666666", Value = "Option A" },
                                new FieldOption { OptionId = "77777777-7777-7777-7777-777777777777", Value = "Option B" }
                            }
                        },
                        new FormField
                        {
                            QuestionId = "88888888-8888-8888-8888-888888888888",
                            Label = "Text Question",
                            Type = "text"
                        },
                        new FormField
                        {
                            QuestionId = "99999999-9999-9999-9999-999999999999",
                            Label = "File Upload Question",
                            Type = "file"
                        }
                    }
                }
            };

            // Setup the mock to return our form when GetFormByIdAsync is called
            _mockFormBL.Setup(m => m.GetFormByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(form));
        }

        [Fact]
        public async Task SubmitResponseAsync_ValidResponse_ReturnsResponseId()
        {
            // Arrange
            var responseDto = new ResponseDTO
            {
                FormId = "11111111-1111-1111-1111-111111111111",
                SubmittedBy = "testuser",
                Answers = new List<ResponseAnswerDTO>
                {
                    new ResponseAnswerDTO
                    {
                        QuestionId = "22222222-2222-2222-2222-222222222222",
                        AnswerText = "Option 1"
                    }
                }
            };

            // Act
            var result = await _responseBL.SubmitResponseAsync(responseDto);

            // Assert
            Assert.NotEqual(0, result);
            var savedResponse = await _context.FormResponses.FindAsync(result);
            Assert.NotNull(savedResponse);
            Assert.Equal(responseDto.FormId, savedResponse.FormId);
            Assert.Equal(responseDto.SubmittedBy, savedResponse.SubmittedBy);
        }

        [Fact]
        public async Task SubmitResponseAsync_NullDto_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _responseBL.SubmitResponseAsync(null!));
        }

        [Fact]
        public async Task GetResponsesForFormAsync_ReturnsAllResponses()
        {
            // Arrange
            var formId = "11111111-1111-1111-1111-111111111111";
            
            // Add some responses
            await _context.FormResponses.AddRangeAsync(
                new FormResponse { FormId = formId, SubmittedBy = "user1", SubmittedAt = DateTime.UtcNow },
                new FormResponse { FormId = formId, SubmittedBy = "user2", SubmittedAt = DateTime.UtcNow },
                new FormResponse { FormId = "different-form", SubmittedBy = "user3", SubmittedAt = DateTime.UtcNow } // Different form
            );
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetResponsesForFormAsync(formId);

            // Assert
            Assert.Equal(2, responses.Count);
            Assert.All(responses, r => Assert.Equal(formId, r.FormId));
        }

        [Fact]
        public async Task GetResponsesForFormAsync_NoResponses_ReturnsEmptyList()
        {
            // Arrange
            var formId = "nonexistent-form";

            // Act
            var responses = await _responseBL.GetResponsesForFormAsync(formId);

            // Assert
            Assert.Empty(responses);
        }

        [Fact]
        public async Task GetResponsesForUserAsync_ReturnsUserResponses()
        {
            // Arrange
            var formId = "11111111-1111-1111-1111-111111111111";
            var userId = "testuser123";
            
            // Add some responses
            await _context.FormResponses.AddRangeAsync(
                new FormResponse { FormId = formId, SubmittedBy = userId, SubmittedAt = DateTime.UtcNow },
                new FormResponse { FormId = formId, SubmittedBy = userId, SubmittedAt = DateTime.UtcNow },
                new FormResponse { FormId = formId, SubmittedBy = "otheruser", SubmittedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetResponsesForUserAsync(formId, userId);

            // Assert
            Assert.Equal(2, responses.Count);
            Assert.All(responses, r => Assert.Equal(userId, r.SubmittedBy));
        }

        [Fact]
        public async Task GetResponsesForUserAsync_NoResponses_ReturnsEmptyList()
        {
            // Arrange
            var formId = "11111111-1111-1111-1111-111111111111";
            var userId = "nonexistentuser";

            // Act
            var responses = await _responseBL.GetResponsesForUserAsync(formId, userId);

            // Assert
            Assert.Empty(responses);
        }

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_FileExists_ReturnsFileDTO()
        {
            // Arrange
            // Create a response
            var response = new FormResponse
            {
                FormId = "11111111-1111-1111-1111-111111111111",
                SubmittedBy = "testuser",
                SubmittedAt = DateTime.UtcNow
            };
            
            _context.FormResponses.Add(response);
            await _context.SaveChangesAsync();
            
            // Add a file to the response
            var responseFile = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "99999999-9999-9999-9999-999999999999",
                FileName = "test.txt",
                FileType = "text/plain",
                FileMaxSize = 1024,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test file content")),
                UploadedAt = DateTime.UtcNow
            };
            
            _context.ResponseFiles.Add(responseFile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _responseBL.GetFileByResponseIdAndFileIdAsync(response.ResponseId, responseFile.FileId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(responseFile.FileName, result.FileName);
            Assert.Equal(responseFile.FileType, result.FileType);
            Assert.Equal(responseFile.Base64Content, result.Base64Content);
        }

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_FileNotFound_ReturnsNull()
        {
            // Arrange
            var responseId = 999;
            var fileId = 999;

            // Act
            var result = await _responseBL.GetFileByResponseIdAndFileIdAsync(responseId, fileId);

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            // Clean up the in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
