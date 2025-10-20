using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Helper;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FormBuilderAPITests.BusinessLogicTest
{
    public class AuthBLTest : IDisposable
    {
        private readonly SQLDbContext _context;
        private readonly IAuthBL _authBL;
        private readonly string _databaseName;
        private readonly PasswordHasher _passwordHasher;

        public AuthBLTest()
        {
            // Setup in-memory database
            _databaseName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<SQLDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;
            _context = new SQLDbContext(options);

            // Create a real configuration object
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "ThisIsAVeryLongTestSecretKeyForJwtTokenGeneration1234567890"},
                {"Jwt:Issuer", "test-issuer"},
                {"Jwt:Audience", "test-audience"},
                {"Jwt:ExpireMinutes", "120"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Initialize AuthBL
            _authBL = new AuthBL(_context, configuration);
            
            // Create password hasher for test data preparation
            _passwordHasher = new PasswordHasher();

            // Seed the database
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            // Add test users with correctly hashed passwords using the same PasswordHasher class
            var users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    PasswordHash = _passwordHasher.HashPassword("admin123"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 2,
                    Username = "learner",
                    PasswordHash = _passwordHasher.HashPassword("learner123"),
                    Role = "Learner",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "admin",
                Password = "admin123"
            };

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token);
            Assert.Equal("Admin", result.Role);
            Assert.Equal("admin", result.Username);
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ReturnsNull()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "nonexistent",
                Password = "password"
            };

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "admin",
                Password = "wrongpassword"
            };

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_ReturnsAuthResponse()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "newuser",
                Password = "password123",
                Role = "Learner"
            };

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token);
            Assert.Equal("Learner", result.Role);
            Assert.Equal("newuser", result.Username);
            
            // Verify user was added to database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            Assert.NotNull(user);
            
            // Verify password was correctly hashed
            Assert.True(_passwordHasher.VerifyPassword("password123", user.PasswordHash));
        }

        [Fact]
        public async Task RegisterAsync_ExistingUsername_ReturnsNull()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "admin", // Already exists
                Password = "password123",
                Role = "Learner"
            };

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ExistingUser_ReturnsUser()
        {
            // Act
            var result = await _authBL.GetUserByUsernameAsync("admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin", result.Username);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_NonExistentUser_ReturnsNull()
        {
            // Act
            var result = await _authBL.GetUserByUsernameAsync("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_ReturnsTrue()
        {
            // This is a placeholder test since the method always returns true
            var result = await _authBL.ValidateTokenAsync("any-token");
            Assert.True(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
