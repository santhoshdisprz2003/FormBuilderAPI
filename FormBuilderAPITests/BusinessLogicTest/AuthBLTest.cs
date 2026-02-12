using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Helper;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Repository;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FormBuilderAPITests.BusinessLogicTest
{
    public class AuthBLTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly IAuthBL _authBL;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher _passwordHasher;

        public AuthBLTest()
        {
            // Setup mock repository
            _mockUserRepository = new Mock<IUserRepository>();

            // Create a real configuration object
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "ThisIsAVeryLongTestSecretKeyForJwtTokenGeneration1234567890"},
                {"Jwt:Issuer", "test-issuer"},
                {"Jwt:Audience", "test-audience"},
                {"Jwt:ExpireMinutes", "120"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Initialize AuthBL with mocked repository
            _authBL = new AuthBL(_mockUserRepository.Object, _configuration);
            
            // Create password hasher for test data preparation
            _passwordHasher = new PasswordHasher();
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

            var user = new User
            {
                UserId = 1,
                Username = "admin",
                PasswordHash = _passwordHasher.HashPassword("admin123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(repo => repo.GetUserByUsernameAsync("admin"))
                .ReturnsAsync(user);

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token);
            Assert.Equal("Admin", result.Role);
            Assert.Equal("admin", result.Username);
            Assert.Equal("1", result.UserId);
            
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync("admin"), Times.Once);
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

            _mockUserRepository
                .Setup(repo => repo.GetUserByUsernameAsync("nonexistent"))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync("nonexistent"), Times.Once);
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

            var user = new User
            {
                UserId = 1,
                Username = "admin",
                PasswordHash = _passwordHasher.HashPassword("admin123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(repo => repo.GetUserByUsernameAsync("admin"))
                .ReturnsAsync(user);

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync("admin"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_EmptyUsername_ReturnsNull()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "",
                Password = "password123"
            };

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_EmptyPassword_ReturnsNull()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "admin",
                Password = ""
            };

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
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

            var savedUser = new User
            {
                UserId = 3,
                Username = "newuser",
                PasswordHash = _passwordHasher.HashPassword("password123"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(repo => repo.UserExistsAsync("newuser"))
                .ReturnsAsync(false);

            _mockUserRepository
                .Setup(repo => repo.InsertUserAsync(It.IsAny<User>()))
                .ReturnsAsync(savedUser);

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token);
            Assert.Equal("Learner", result.Role);
            Assert.Equal("newuser", result.Username);
            Assert.Equal("3", result.UserId);
            
            _mockUserRepository.Verify(repo => repo.UserExistsAsync("newuser"), Times.Once);
            _mockUserRepository.Verify(repo => repo.InsertUserAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ExistingUsername_ReturnsNull()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "admin",
                Password = "password123",
                Role = "Learner"
            };

            _mockUserRepository
                .Setup(repo => repo.UserExistsAsync("admin"))
                .ReturnsAsync(true);

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.UserExistsAsync("admin"), Times.Once);
            _mockUserRepository.Verify(repo => repo.InsertUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_EmptyUsername_ReturnsNull()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "",
                Password = "password123",
                Role = "Learner"
            };

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.UserExistsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_EmptyPassword_ReturnsNull()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "newuser",
                Password = "",
                Role = "Learner"
            };

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.UserExistsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_NoRoleSpecified_DefaultsToLearner()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "newuser",
                Password = "password123",
                Role = null
            };

            var savedUser = new User
            {
                UserId = 4,
                Username = "newuser",
                PasswordHash = _passwordHasher.HashPassword("password123"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(repo => repo.UserExistsAsync("newuser"))
                .ReturnsAsync(false);

            _mockUserRepository
                .Setup(repo => repo.InsertUserAsync(It.IsAny<User>()))
                .ReturnsAsync(savedUser);

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Learner", result.Role);
        }

        [Fact]
        public async Task RegisterAsync_AdminRole_CreatesAdminUser()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "newadmin",
                Password = "password123",
                Role = "Admin"
            };

            var savedUser = new User
            {
                UserId = 5,
                Username = "newadmin",
                PasswordHash = _passwordHasher.HashPassword("password123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(repo => repo.UserExistsAsync("newadmin"))
                .ReturnsAsync(false);

            _mockUserRepository
                .Setup(repo => repo.InsertUserAsync(It.IsAny<User>()))
                .ReturnsAsync(savedUser);

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Username = "admin",
                PasswordHash = _passwordHasher.HashPassword("admin123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(repo => repo.GetUserByUsernameAsync("admin"))
                .ReturnsAsync(user);

            // Act
            var result = await _authBL.GetUserByUsernameAsync("admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin", result.Username);
            Assert.Equal("Admin", result.Role);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync("admin"), Times.Once);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_NonExistentUser_ReturnsNull()
        {
            // Arrange
            _mockUserRepository
                .Setup(repo => repo.GetUserByUsernameAsync("nonexistent"))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authBL.GetUserByUsernameAsync("nonexistent");

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync("nonexistent"), Times.Once);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_EmptyUsername_ReturnsNull()
        {
            // Act
            var result = await _authBL.GetUserByUsernameAsync("");

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateTokenAsync_ReturnsTrue()
        {
            // Act
            var result = await _authBL.ValidateTokenAsync("any-token");

            // Assert
            Assert.True(result);
        }
    }
}
