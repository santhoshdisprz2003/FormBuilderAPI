using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FormBuilderAPITests.RepositoryTest
{
    public class ResponseRepositoryTest : IDisposable
    {
        private readonly SQLDbContext _sqlContext;
        private readonly ResponseRepository _repository;

        public ResponseRepositoryTest()
        {
            _sqlContext = CreateSqlContext();
            _repository = new ResponseRepository(_sqlContext);
        }

        private SQLDbContext CreateSqlContext()
        {
            var options = new DbContextOptionsBuilder<SQLDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new SQLDbContext(options);
        }

        public void Dispose()
        {
            _sqlContext?.Dispose();
        }

        #region InsertResponseAsync Tests

        [Fact]
        public async Task InsertResponseAsync_ValidResponse_ReturnsResponseId()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer
                    {
                        QuestionId = "q1",
                        AnswerText = "Answer 1"
                    },
                    new FormResponseAnswer
                    {
                        QuestionId = "q2",
                        AnswerText = "Answer 2"
                    }
                }
            };

            // Act
            var result = await _repository.InsertResponseAsync(response);

            // Assert
            Assert.True(result > 0);
            Assert.Equal(result, response.ResponseId);

            var savedResponse = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.ResponseId == result);

            Assert.NotNull(savedResponse);
            Assert.Equal("form123", savedResponse.FormId);
            Assert.Equal("user1", savedResponse.SubmittedBy);
            Assert.Equal(2, savedResponse.Answers.Count);
        }

        [Fact]
        public async Task InsertResponseAsync_ResponseWithoutAnswers_ReturnsResponseId()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form456",
                SubmittedBy = "user2",
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>()
            };

            // Act
            var result = await _repository.InsertResponseAsync(response);

            // Assert
            Assert.True(result > 0);
            var savedResponse = await _sqlContext.FormResponses.FindAsync(result);
            Assert.NotNull(savedResponse);
            Assert.Equal("form456", savedResponse.FormId);
        }

        #endregion

        #region GetResponsesByFormIdAsync Tests

        [Fact]
        public async Task GetResponsesByFormIdAsync_ExistingFormId_ReturnsResponses()
        {
            // Arrange
            var formId = "form123";
            await SeedResponsesAsync(formId, "user1", 2);
            await SeedResponsesAsync("form456", "user2", 1);

            // Act
            var result = await _repository.GetResponsesByFormIdAsync(formId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(formId, r.FormId));
            Assert.All(result, r => Assert.NotEmpty(r.Answers));
        }

        [Fact]
        public async Task GetResponsesByFormIdAsync_NonExistingFormId_ReturnsEmptyList()
        {
            // Arrange
            await SeedResponsesAsync("form123", "user1", 1);

            // Act
            var result = await _repository.GetResponsesByFormIdAsync("nonexistent");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetResponsesByFormIdAsync_IncludesAnswers()
        {
            // Arrange
            var formId = "form789";
            var response = new FormResponse
            {
                FormId = formId,
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer { QuestionId = "q1", AnswerText = "Answer 1" },
                    new FormResponseAnswer { QuestionId = "q2", AnswerText = "Answer 2" }
                }
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetResponsesByFormIdAsync(formId);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].Answers.Count);
        }

        #endregion

        #region GetResponsesByUserIdAsync Tests

        [Fact]
        public async Task GetResponsesByUserIdAsync_ExistingUserId_ReturnsResponses()
        {
            // Arrange
            var userId = "user1";
            await SeedResponsesAsync("form123", userId, 2);
            await SeedResponsesAsync("form456", "user2", 1);

            // Act
            var result = await _repository.GetResponsesByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userId, r.SubmittedBy));
        }

        [Fact]
        public async Task GetResponsesByUserIdAsync_NonExistingUserId_ReturnsEmptyList()
        {
            // Arrange
            await SeedResponsesAsync("form123", "user1", 1);

            // Act
            var result = await _repository.GetResponsesByUserIdAsync("nonexistent");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetResponsesByUserIdAsync_MultipleFormsForSameUser_ReturnsAllResponses()
        {
            // Arrange
            var userId = "user1";
            var response1 = new FormResponse
            {
                FormId = "form1",
                SubmittedBy = userId,
                Answers = new List<FormResponseAnswer>()
            };
            var response2 = new FormResponse
            {
                FormId = "form2",
                SubmittedBy = userId,
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.AddRange(response1, response2);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetResponsesByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.FormId == "form1");
            Assert.Contains(result, r => r.FormId == "form2");
        }

        #endregion

        #region GetResponsesByFormIdAndUserIdAsync Tests

        [Fact]
        public async Task GetResponsesByFormIdAndUserIdAsync_ExistingFormAndUser_ReturnsResponses()
        {
            // Arrange
            var formId = "form123";
            var userId = "user1";
            await SeedResponsesAsync(formId, userId, 2);
            await SeedResponsesAsync(formId, "user2", 1);
            await SeedResponsesAsync("form456", userId, 1);

            // Act
            var result = await _repository.GetResponsesByFormIdAndUserIdAsync(formId, userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(formId, r.FormId));
            Assert.All(result, r => Assert.Equal(userId, r.SubmittedBy));
        }

        [Fact]
        public async Task GetResponsesByFormIdAndUserIdAsync_NonExistingCombination_ReturnsEmptyList()
        {
            // Arrange
            await SeedResponsesAsync("form123", "user1", 1);

            // Act
            var result = await _repository.GetResponsesByFormIdAndUserIdAsync("form123", "user2");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region InsertResponseFileAsync Tests

        [Fact]
        public async Task InsertResponseFileAsync_ValidFile_InsertsSuccessfully()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            var file = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "q1",
                FileName = "test.pdf",
                FileType = "application/pdf",
                FileMaxSize = 1024,
                Base64Content = "base64content",
                UploadedAt = DateTime.UtcNow
            };

            // Act
            await _repository.InsertResponseFileAsync(file);

            // Assert
            var savedFile = await _sqlContext.ResponseFiles
                .FirstOrDefaultAsync(f => f.ResponseId == response.ResponseId);

            Assert.NotNull(savedFile);
            Assert.Equal("test.pdf", savedFile.FileName);
            Assert.Equal("application/pdf", savedFile.FileType);
            Assert.Equal(1024, savedFile.FileMaxSize);
        }

        [Fact]
        public async Task InsertResponseFileAsync_MultipleFiles_InsertsAll()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            var file1 = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "q1",
                FileName = "file1.pdf",
                FileType = "application/pdf",
                FileMaxSize = 1024,
                Base64Content = "content1"
            };

            var file2 = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "q2",
                FileName = "file2.jpg",
                FileType = "image/jpeg",
                FileMaxSize = 2048,
                Base64Content = "content2"
            };

            // Act
            await _repository.InsertResponseFileAsync(file1);
            await _repository.InsertResponseFileAsync(file2);

            // Assert
            var savedFiles = await _sqlContext.ResponseFiles
                .Where(f => f.ResponseId == response.ResponseId)
                .ToListAsync();

            Assert.Equal(2, savedFiles.Count);
        }

        #endregion

        #region GetFilesByResponseIdAsync Tests

        [Fact]
        public async Task GetFilesByResponseIdAsync_ExistingFiles_ReturnsFiles()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            var files = new List<ResponseFile>
            {
                new ResponseFile
                {
                    ResponseId = response.ResponseId,
                    QuestionId = "q1",
                    FileName = "file1.pdf",
                    FileType = "application/pdf",
                    FileMaxSize = 1024,
                    Base64Content = "content1"
                },
                new ResponseFile
                {
                    ResponseId = response.ResponseId,
                    QuestionId = "q2",
                    FileName = "file2.jpg",
                    FileType = "image/jpeg",
                    FileMaxSize = 2048,
                    Base64Content = "content2"
                }
            };
            _sqlContext.ResponseFiles.AddRange(files);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetFilesByResponseIdAsync(response.ResponseId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.FileName == "file1.pdf");
            Assert.Contains(result, f => f.FileName == "file2.jpg");
        }

        [Fact]
        public async Task GetFilesByResponseIdAsync_NoFiles_ReturnsEmptyList()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetFilesByResponseIdAsync(response.ResponseId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFilesByResponseIdAsync_NonExistingResponseId_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.GetFilesByResponseIdAsync(999);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetFileByResponseIdAndFileIdAsync Tests

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_ExistingFile_ReturnsFile()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            var file = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "q1",
                FileName = "test.pdf",
                FileType = "application/pdf",
                FileMaxSize = 1024,
                Base64Content = "content"
            };
            _sqlContext.ResponseFiles.Add(file);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetFileByResponseIdAndFileIdAsync(response.ResponseId, file.FileId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal(response.ResponseId, result.ResponseId);
        }

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_NonExistingFile_ReturnsNull()
        {
            // Act
            var result = await _repository.GetFileByResponseIdAndFileIdAsync(999, 999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileByResponseIdAndFileIdAsync_WrongResponseId_ReturnsNull()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            var file = new ResponseFile
            {
                ResponseId = response.ResponseId,
                QuestionId = "q1",
                FileName = "test.pdf",
                FileType = "application/pdf",
                FileMaxSize = 1024,
                Base64Content = "content"
            };
            _sqlContext.ResponseFiles.Add(file);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetFileByResponseIdAndFileIdAsync(999, file.FileId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUserNamesByIdsAsync Tests

        [Fact]
        public async Task GetUserNamesByIdsAsync_ExistingUsers_ReturnsDictionary()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Username = "user1", PasswordHash = "hash1", Role = "Learner" },
                new User { UserId = 2, Username = "user2", PasswordHash = "hash2", Role = "Learner" },
                new User { UserId = 3, Username = "user3", PasswordHash = "hash3", Role = "Admin" }
            };
            _sqlContext.Users.AddRange(users);
            await _sqlContext.SaveChangesAsync();

            var userIds = new List<string> { "1", "2", "3" };

            // Act
            var result = await _repository.GetUserNamesByIdsAsync(userIds);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("user1", result["1"]);
            Assert.Equal("user2", result["2"]);
            Assert.Equal("user3", result["3"]);
        }

        [Fact]
        public async Task GetUserNamesByIdsAsync_PartialMatch_ReturnsOnlyMatchingUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Username = "user1", PasswordHash = "hash1", Role = "Learner" },
                new User { UserId = 2, Username = "user2", PasswordHash = "hash2", Role = "Learner" }
            };
            _sqlContext.Users.AddRange(users);
            await _sqlContext.SaveChangesAsync();

            var userIds = new List<string> { "1", "2", "999" };

            // Act
            var result = await _repository.GetUserNamesByIdsAsync(userIds);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("user1", result["1"]);
            Assert.Equal("user2", result["2"]);
            Assert.False(result.ContainsKey("999"));
        }

        [Fact]
        public async Task GetUserNamesByIdsAsync_NoMatchingUsers_ReturnsEmptyDictionary()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Username = "user1", PasswordHash = "hash1", Role = "Learner" }
            };
            _sqlContext.Users.AddRange(users);
            await _sqlContext.SaveChangesAsync();

            var userIds = new List<string> { "999", "888" };

            // Act
            var result = await _repository.GetUserNamesByIdsAsync(userIds);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUserNamesByIdsAsync_EmptyList_ReturnsEmptyDictionary()
        {
            // Arrange
            var userIds = new List<string>();

            // Act
            var result = await _repository.GetUserNamesByIdsAsync(userIds);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithPendingChanges_SavesSuccessfully()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                Answers = new List<FormResponseAnswer>()
            };
            _sqlContext.FormResponses.Add(response);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            var savedResponse = await _sqlContext.FormResponses.FirstOrDefaultAsync();
            Assert.NotNull(savedResponse);
            Assert.Equal("form123", savedResponse.FormId);
        }

        [Fact]
        public async Task SaveChangesAsync_NoPendingChanges_CompletesSuccessfully()
        {
            // Act & Assert
            await _repository.SaveChangesAsync(); // Should not throw
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteResponseFlow_InsertResponseWithFilesAndRetrieve_WorksCorrectly()
        {
            // Arrange
            var response = new FormResponse
            {
                FormId = "form123",
                SubmittedBy = "user1",
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<FormResponseAnswer>
                {
                    new FormResponseAnswer { QuestionId = "q1", AnswerText = "Answer 1" },
                    new FormResponseAnswer { QuestionId = "q2", AnswerText = "Answer 2" }
                }
            };

            // Act - Insert Response
            var responseId = await _repository.InsertResponseAsync(response);

            // Act - Insert Files
            var file1 = new ResponseFile
            {
                ResponseId = responseId,
                QuestionId = "q3",
                FileName = "document.pdf",
                FileType = "application/pdf",
                FileMaxSize = 2048,
                Base64Content = "base64content1"
            };
            await _repository.InsertResponseFileAsync(file1);

            // Act - Retrieve Response
            var responses = await _repository.GetResponsesByFormIdAsync("form123");
            var files = await _repository.GetFilesByResponseIdAsync(responseId);

            // Assert
            Assert.Single(responses);
            Assert.Equal(2, responses[0].Answers.Count);
            Assert.Single(files);
            Assert.Equal("document.pdf", files[0].FileName);
        }

        [Fact]
        public async Task MultipleUsersMultipleForms_RetrievalWorks()
        {
            // Arrange
            await SeedResponsesAsync("form1", "user1", 2);
            await SeedResponsesAsync("form1", "user2", 1);
            await SeedResponsesAsync("form2", "user1", 1);

            // Act
            var form1Responses = await _repository.GetResponsesByFormIdAsync("form1");
            var user1Responses = await _repository.GetResponsesByUserIdAsync("user1");
            var form1User1Responses = await _repository.GetResponsesByFormIdAndUserIdAsync("form1", "user1");

            // Assert
            Assert.Equal(3, form1Responses.Count);
            Assert.Equal(3, user1Responses.Count);
            Assert.Equal(2, form1User1Responses.Count);
        }

        #endregion

        #region Helper Methods

        private async Task SeedResponsesAsync(string formId, string userId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var response = new FormResponse
                {
                    FormId = formId,
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-i),
                    Answers = new List<FormResponseAnswer>
                    {
                        new FormResponseAnswer
                        {
                            QuestionId = $"q{i}",
                            AnswerText = $"Answer {i}"
                        }
                    }
                };
                _sqlContext.FormResponses.Add(response);
            }
            await _sqlContext.SaveChangesAsync();
        }

        #endregion
    }
}
