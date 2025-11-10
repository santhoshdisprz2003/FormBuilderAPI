using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FormBuilderAPITests.RepositoryTest
{
    public class UserRepositoryTest : IDisposable
    {
        private readonly SQLDbContext _sqlContext;
        private readonly UserRepository _repository;

        public UserRepositoryTest()
        {
            _sqlContext = CreateSqlContext();
            _repository = new UserRepository(_sqlContext);
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

        #region GetUserByUsernameAsync Tests

        [Fact]
        public async Task GetUserByUsernameAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword123",
                Role = "Learner",
                CreatedAt = DateTime.UtcNow
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByUsernameAsync("testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("hashedpassword123", result.PasswordHash);
            Assert.Equal("Learner", result.Role);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Username = "existinguser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByUsernameAsync("nonexistinguser");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_CaseInsensitive_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Username = "TestUser",
                PasswordHash = "hash",
                Role = "Admin"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetUserByUsernameAsync("testuser");
            var result2 = await _repository.GetUserByUsernameAsync("TESTUSER");
            var result3 = await _repository.GetUserByUsernameAsync("TestUser");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.Equal("TestUser", result1.Username);
            Assert.Equal("TestUser", result2.Username);
            Assert.Equal("TestUser", result3.Username);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_EmptyDatabase_ReturnsNull()
        {
            // Act
            var result = await _repository.GetUserByUsernameAsync("anyuser");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_MultipleUsers_ReturnsCorrectUser()
        {
            // Arrange
            var users = new[]
            {
                new User { Username = "user1", PasswordHash = "hash1", Role = "Learner" },
                new User { Username = "user2", PasswordHash = "hash2", Role = "Admin" },
                new User { Username = "user3", PasswordHash = "hash3", Role = "Learner" }
            };
            _sqlContext.Users.AddRange(users);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByUsernameAsync("user2");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user2", result.Username);
            Assert.Equal("hash2", result.PasswordHash);
            Assert.Equal("Admin", result.Role);
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_ExistingUserId_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByIdAsync(user.UserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public async Task GetUserByIdAsync_NonExistingUserId_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_MultipleUsers_ReturnsCorrectUser()
        {
            // Arrange
            var user1 = new User { Username = "user1", PasswordHash = "hash1", Role = "Learner" };
            var user2 = new User { Username = "user2", PasswordHash = "hash2", Role = "Admin" };
            var user3 = new User { Username = "user3", PasswordHash = "hash3", Role = "Learner" };
            
            _sqlContext.Users.AddRange(user1, user2, user3);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByIdAsync(user2.UserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user2.UserId, result.UserId);
            Assert.Equal("user2", result.Username);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task GetUserByIdAsync_EmptyDatabase_ReturnsNull()
        {
            // Act
            var result = await _repository.GetUserByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region InsertUserAsync Tests

        [Fact]
        public async Task InsertUserAsync_ValidUser_ReturnsUserWithId()
        {
            // Arrange
            var user = new User
            {
                Username = "newuser",
                PasswordHash = "secureHash123",
                Role = "Learner",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.InsertUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.UserId > 0);
            Assert.Equal("newuser", result.Username);
            Assert.Equal("secureHash123", result.PasswordHash);
            Assert.Equal("Learner", result.Role);

            // Verify in database
            var savedUser = await _sqlContext.Users.FindAsync(result.UserId);
            Assert.NotNull(savedUser);
            Assert.Equal("newuser", savedUser.Username);
        }

        [Fact]
        public async Task InsertUserAsync_AdminUser_InsertsSuccessfully()
        {
            // Arrange
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = "adminHash",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.InsertUserAsync(adminUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Admin", result.Role);
            Assert.True(result.UserId > 0);
        }

        [Fact]
        public async Task InsertUserAsync_MultipleUsers_AssignsUniqueIds()
        {
            // Arrange
            var user1 = new User { Username = "user1", PasswordHash = "hash1", Role = "Learner" };
            var user2 = new User { Username = "user2", PasswordHash = "hash2", Role = "Learner" };
            var user3 = new User { Username = "user3", PasswordHash = "hash3", Role = "Admin" };

            // Act
            var result1 = await _repository.InsertUserAsync(user1);
            var result2 = await _repository.InsertUserAsync(user2);
            var result3 = await _repository.InsertUserAsync(user3);

            // Assert
            Assert.True(result1.UserId > 0);
            Assert.True(result2.UserId > 0);
            Assert.True(result3.UserId > 0);
            Assert.NotEqual(result1.UserId, result2.UserId);
            Assert.NotEqual(result2.UserId, result3.UserId);
            Assert.NotEqual(result1.UserId, result3.UserId);
        }

        [Fact]
        public async Task InsertUserAsync_PreservesCreatedAt()
        {
            // Arrange
            var specificTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var user = new User
            {
                Username = "timeuser",
                PasswordHash = "hash",
                Role = "Learner",
                CreatedAt = specificTime
            };

            // Act
            var result = await _repository.InsertUserAsync(user);

            // Assert
            Assert.Equal(specificTime, result.CreatedAt);
        }

        #endregion

        #region UserExistsAsync Tests

        [Fact]
        public async Task UserExistsAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                Username = "existinguser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.UserExistsAsync("existinguser");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserExistsAsync_NonExistingUser_ReturnsFalse()
        {
            // Arrange
            var user = new User
            {
                Username = "existinguser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.UserExistsAsync("nonexistinguser");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserExistsAsync_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                Username = "TestUser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result1 = await _repository.UserExistsAsync("testuser");
            var result2 = await _repository.UserExistsAsync("TESTUSER");
            var result3 = await _repository.UserExistsAsync("TestUser");

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
        }

        [Fact]
        public async Task UserExistsAsync_EmptyDatabase_ReturnsFalse()
        {
            // Act
            var result = await _repository.UserExistsAsync("anyuser");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserExistsAsync_MultipleUsers_ChecksCorrectly()
        {
            // Arrange
            var users = new[]
            {
                new User { Username = "user1", PasswordHash = "hash1", Role = "Learner" },
                new User { Username = "user2", PasswordHash = "hash2", Role = "Admin" },
                new User { Username = "user3", PasswordHash = "hash3", Role = "Learner" }
            };
            _sqlContext.Users.AddRange(users);
            await _sqlContext.SaveChangesAsync();

            // Act
            var exists1 = await _repository.UserExistsAsync("user1");
            var exists2 = await _repository.UserExistsAsync("user2");
            var exists3 = await _repository.UserExistsAsync("user3");
            var existsNot = await _repository.UserExistsAsync("user4");

            // Assert
            Assert.True(exists1);
            Assert.True(exists2);
            Assert.True(exists3);
            Assert.False(existsNot);
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithPendingChanges_SavesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            var savedUser = await _sqlContext.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
            Assert.NotNull(savedUser);
            Assert.Equal("testuser", savedUser.Username);
        }

        [Fact]
        public async Task SaveChangesAsync_NoPendingChanges_CompletesSuccessfully()
        {
            // Act & Assert
            await _repository.SaveChangesAsync(); // Should not throw
        }

        [Fact]
        public async Task SaveChangesAsync_MultipleChanges_SavesAll()
        {
            // Arrange
            var user1 = new User { Username = "user1", PasswordHash = "hash1", Role = "Learner" };
            var user2 = new User { Username = "user2", PasswordHash = "hash2", Role = "Admin" };
            
            _sqlContext.Users.Add(user1);
            _sqlContext.Users.Add(user2);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            var count = await _sqlContext.Users.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task SaveChangesAsync_UpdateExistingUser_SavesChanges()
        {
            // Arrange
            var user = new User
            {
                Username = "originalname",
                PasswordHash = "originalhash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            user.PasswordHash = "newhash";
            await _repository.SaveChangesAsync();

            // Assert
            var updatedUser = await _sqlContext.Users.FindAsync(user.UserId);
            Assert.NotNull(updatedUser);
            Assert.Equal("newhash", updatedUser.PasswordHash);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteUserFlow_CreateCheckAndRetrieve_WorksCorrectly()
        {
            // Arrange
            var username = "integrationuser";
            var user = new User
            {
                Username = username,
                PasswordHash = "secureHash",
                Role = "Learner",
                CreatedAt = DateTime.UtcNow
            };

            // Act - Check user doesn't exist
            var existsBefore = await _repository.UserExistsAsync(username);

            // Act - Insert user
            var insertedUser = await _repository.InsertUserAsync(user);

            // Act - Check user exists
            var existsAfter = await _repository.UserExistsAsync(username);

            // Act - Retrieve by username
            var retrievedByUsername = await _repository.GetUserByUsernameAsync(username);

            // Act - Retrieve by ID
            var retrievedById = await _repository.GetUserByIdAsync(insertedUser.UserId);

            // Assert
            Assert.False(existsBefore);
            Assert.True(existsAfter);
            Assert.NotNull(retrievedByUsername);
            Assert.NotNull(retrievedById);
            Assert.Equal(insertedUser.UserId, retrievedByUsername.UserId);
            Assert.Equal(insertedUser.UserId, retrievedById.UserId);
            Assert.Equal(username, retrievedByUsername.Username);
            Assert.Equal(username, retrievedById.Username);
        }

        [Fact]
        public async Task MultipleUsersWithDifferentRoles_AllOperationsWork()
        {
            // Arrange & Act
            var learner = await _repository.InsertUserAsync(new User
            {
                Username = "learner1",
                PasswordHash = "hash1",
                Role = "Learner"
            });

            var admin = await _repository.InsertUserAsync(new User
            {
                Username = "admin1",
                PasswordHash = "hash2",
                Role = "Admin"
            });

            // Assert
            var learnerExists = await _repository.UserExistsAsync("learner1");
            var adminExists = await _repository.UserExistsAsync("admin1");
            var learnerRetrieved = await _repository.GetUserByIdAsync(learner.UserId);
            var adminRetrieved = await _repository.GetUserByIdAsync(admin.UserId);

            Assert.True(learnerExists);
            Assert.True(adminExists);
            Assert.Equal("Learner", learnerRetrieved.Role);
            Assert.Equal("Admin", adminRetrieved.Role);
        }

        [Fact]
        public async Task UsernameCaseSensitivity_ConsistentBehavior()
        {
            // Arrange
            var user = new User
            {
                Username = "MixedCaseUser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            await _repository.InsertUserAsync(user);

            // Act
            var existsLower = await _repository.UserExistsAsync("mixedcaseuser");
            var existsUpper = await _repository.UserExistsAsync("MIXEDCASEUSER");
            var existsMixed = await _repository.UserExistsAsync("MixedCaseUser");

            var retrievedLower = await _repository.GetUserByUsernameAsync("mixedcaseuser");
            var retrievedUpper = await _repository.GetUserByUsernameAsync("MIXEDCASEUSER");
            var retrievedMixed = await _repository.GetUserByUsernameAsync("MixedCaseUser");

            // Assert
            Assert.True(existsLower);
            Assert.True(existsUpper);
            Assert.True(existsMixed);
            Assert.NotNull(retrievedLower);
            Assert.NotNull(retrievedUpper);
            Assert.NotNull(retrievedMixed);
            Assert.Equal("MixedCaseUser", retrievedLower.Username); // Original casing preserved
            Assert.Equal("MixedCaseUser", retrievedUpper.Username);
            Assert.Equal("MixedCaseUser", retrievedMixed.Username);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetUserByUsernameAsync_EmptyString_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Username = "validuser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByUsernameAsync("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_NegativeId_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UserExistsAsync_EmptyString_ReturnsFalse()
        {
            // Arrange
            var user = new User
            {
                Username = "validuser",
                PasswordHash = "hash",
                Role = "Learner"
            };
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();

            // Act
            var result = await _repository.UserExistsAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task InsertUserAsync_WithDefaultCreatedAt_UsesUtcNow()
        {
            // Arrange
            var beforeInsert = DateTime.UtcNow;
            var user = new User
            {
                Username = "timetest",
                PasswordHash = "hash",
                Role = "Learner"
                // CreatedAt will use default value from model
            };

            // Act
            var result = await _repository.InsertUserAsync(user);
            var afterInsert = DateTime.UtcNow;

            // Assert
            Assert.True(result.CreatedAt >= beforeInsert);
            Assert.True(result.CreatedAt <= afterInsert);
        }

        #endregion
    }
}
