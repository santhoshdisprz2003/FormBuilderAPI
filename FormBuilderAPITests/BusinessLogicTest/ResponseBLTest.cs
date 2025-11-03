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

            var form2 = new Form
            {
                Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                Config = new FormConfig
                {
                    Title = "Second Test Form",
                    Description = "Second Form Description"
                },
                Layout = new FormLayout
                {
                    Fields = new List<FormField>
                    {
                        new FormField
                        {
                            QuestionId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                            Label = "Text Question",
                            Type = "text"
                        }
                    }
                }
            };

            // Setup the mock to return our forms when GetFormByIdAsync is called
            _mockFormBL.Setup(m => m.GetFormByIdAsync("11111111-1111-1111-1111-111111111111", It.IsAny<string>()))
                .Returns(Task.FromResult(form));
            
            _mockFormBL.Setup(m => m.GetFormByIdAsync("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", It.IsAny<string>()))
                .Returns(Task.FromResult(form2));
            
            // Setup for unknown forms
            _mockFormBL.Setup(m => m.GetFormByIdAsync("unknown-form-id", It.IsAny<string>()))
                .ThrowsAsync(new Exception("Form not found"));
        }

        #region SubmitResponseAsync Tests

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

        #endregion

        #region GetResponsesForFormAsync Tests

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

        #endregion

        #region GetResponsesForUserAsync Tests

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
        public async Task GetResponsesForUserAsync_WithAnswers_ReturnsResponsesWithAnswers()
        {
            // Arrange
            var formId = "11111111-1111-1111-1111-111111111111";
            var userId = "testuser123";
            
            var response = new FormResponse 
            { 
                FormId = formId, 
                SubmittedBy = userId, 
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer
                    {
                        QuestionId = "22222222-2222-2222-2222-222222222222",
                        AnswerText = "Test Answer"
                    }
                }
            };
            
            await _context.FormResponses.AddAsync(response);
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetResponsesForUserAsync(formId, userId);

            // Assert
            Assert.Single(responses);
            Assert.Single(responses[0].Answers);
            Assert.Equal("Test Answer", responses[0].Answers[0].AnswerText);
        }

        #endregion

        #region GetAllResponsesByUserAsync Tests

        [Fact]
        public async Task GetAllResponsesByUserAsync_UserHasResponses_ReturnsAllResponses()
        {
            // Arrange
            var userId = "testuser123";
            var formId1 = "11111111-1111-1111-1111-111111111111";
            var formId2 = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            
            // Add responses for multiple forms
            await _context.FormResponses.AddRangeAsync(
                new FormResponse 
                { 
                    FormId = formId1, 
                    SubmittedBy = userId, 
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { QuestionId = "q1", AnswerText = "answer1" }
                    }
                },
                new FormResponse 
                { 
                    FormId = formId2, 
                    SubmittedBy = userId, 
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { QuestionId = "q2", AnswerText = "answer2" }
                    }
                },
                new FormResponse 
                { 
                    FormId = formId1, 
                    SubmittedBy = "otheruser", 
                    SubmittedAt = DateTime.UtcNow 
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetAllResponsesByUserAsync(userId);

            // Assert
            Assert.Equal(2, responses.Count);
            Assert.All(responses, r => Assert.Equal(userId, r.SubmittedBy));
            
            // Verify form titles are populated
            var response1 = responses.FirstOrDefault(r => r.FormId == formId1);
            var response2 = responses.FirstOrDefault(r => r.FormId == formId2);
            
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.Equal("Test Form", response1.FormTitle);
            Assert.Equal("Second Test Form", response2.FormTitle);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_NoResponses_ReturnsEmptyList()
        {
            // Arrange
            var userId = "nonexistentuser";

            // Act
            var responses = await _responseBL.GetAllResponsesByUserAsync(userId);

            // Assert
            Assert.Empty(responses);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_WithFiles_ReturnsResponsesWithFiles()
        {
            // Arrange
            var userId = "testuser123";
            var formId = "11111111-1111-1111-1111-111111111111";
            
            var response = new FormResponse 
            { 
                FormId = formId, 
                SubmittedBy = userId, 
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer { QuestionId = "q1", AnswerText = "answer1" }
                }
            };
            
            await _context.FormResponses.AddAsync(response);
            await _context.SaveChangesAsync();
            
            // Add a file
            var file = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "99999999-9999-9999-9999-999999999999",
                FileName = "test.pdf",
                FileType = "application/pdf",
                FileMaxSize = 1024,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test content")),
                UploadedAt = DateTime.UtcNow
            };
            
            await _context.ResponseFiles.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetAllResponsesByUserAsync(userId);

            // Assert
            Assert.Single(responses);
            Assert.Single(responses[0].Files);
            Assert.Equal("test.pdf", responses[0].Files[0].FileName);
            Assert.Equal("application/pdf", responses[0].Files[0].FileType);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_MultipleResponsesSameForm_ReturnsAllResponses()
        {
            // Arrange
            var userId = "testuser123";
            var formId = "11111111-1111-1111-1111-111111111111";
            
            // Add multiple responses for the same form
            await _context.FormResponses.AddRangeAsync(
                new FormResponse 
                { 
                    FormId = formId, 
                    SubmittedBy = userId, 
                    SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { QuestionId = "q1", AnswerText = "first answer" }
                    }
                },
                new FormResponse 
                { 
                    FormId = formId, 
                    SubmittedBy = userId, 
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { QuestionId = "q1", AnswerText = "second answer" }
                    }
                },
                new FormResponse 
                { 
                    FormId = formId, 
                    SubmittedBy = userId, 
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer { QuestionId = "q1", AnswerText = "third answer" }
                    }
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetAllResponsesByUserAsync(userId);

            // Assert
            Assert.Equal(3, responses.Count);
            Assert.All(responses, r => 
            {
                Assert.Equal(userId, r.SubmittedBy);
                Assert.Equal(formId, r.FormId);
                Assert.Equal("Test Form", r.FormTitle);
            });
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_FormNotFound_ReturnsUnknownFormTitle()
        {
            // Arrange
            var userId = "testuser123";
            var unknownFormId = "unknown-form-id";
            
            var response = new FormResponse 
            { 
                FormId = unknownFormId, 
                SubmittedBy = userId, 
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer { QuestionId = "q1", AnswerText = "answer1" }
                }
            };
            
            await _context.FormResponses.AddAsync(response);
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetAllResponsesByUserAsync(userId);

            // Assert
            Assert.Single(responses);
            Assert.Equal("Unknown Form", responses[0].FormTitle);
        }

        [Fact]
        public async Task GetAllResponsesByUserAsync_MixedFormsWithAnswersAndFiles_ReturnsCompleteData()
        {
            // Arrange
            var userId = "testuser123";
            var formId1 = "11111111-1111-1111-1111-111111111111";
            var formId2 = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            
            var response1 = new FormResponse 
            { 
                FormId = formId1, 
                SubmittedBy = userId, 
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer { QuestionId = "q1", AnswerText = "answer1" },
                    new FormResponseAnswer { QuestionId = "q2", AnswerText = "answer2" }
                }
            };
            
            var response2 = new FormResponse 
            { 
                FormId = formId2, 
                SubmittedBy = userId, 
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer { QuestionId = "q3", AnswerText = "answer3" }
                }
            };
            
            await _context.FormResponses.AddRangeAsync(response1, response2);
            await _context.SaveChangesAsync();
            
            // Add files to response1
            var file1 = new ResponseFile
            {
                ResponseId = response1.ResponseId,
                QuestionId = "file-q1",
                FileName = "doc1.pdf",
                FileType = "application/pdf",
                FileMaxSize = 2048,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Content 1")),
                UploadedAt = DateTime.UtcNow
            };
            
            var file2 = new ResponseFile
            {
                ResponseId = response1.ResponseId,
                QuestionId = "file-q2",
                FileName = "doc2.docx",
                FileType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileMaxSize = 3072,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Content 2")),
                UploadedAt = DateTime.UtcNow
            };
            
            await _context.ResponseFiles.AddRangeAsync(file1, file2);
            await _context.SaveChangesAsync();

            // Act
            var responses = await _responseBL.GetAllResponsesByUserAsync(userId);

            // Assert
            Assert.Equal(2, responses.Count);
            
            var resp1 = responses.FirstOrDefault(r => r.FormId == formId1);
            var resp2 = responses.FirstOrDefault(r => r.FormId == formId2);
            
            Assert.NotNull(resp1);
            Assert.NotNull(resp2);
            
            // Verify response 1 has 2 answers and 2 files
            Assert.Equal(2, resp1.Answers.Count);
            Assert.Equal(2, resp1.Files.Count);
            Assert.Equal("Test Form", resp1.FormTitle);
            
            // Verify response 2 has 1 answer and no files
            Assert.Single(resp2.Answers);
            Assert.Empty(resp2.Files);
            Assert.Equal("Second Test Form", resp2.FormTitle);
        }

        #endregion

        #region GetFileByResponseIdAndFileIdAsync Tests

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

        #endregion

        public void Dispose()
        {
            // Clean up the in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
