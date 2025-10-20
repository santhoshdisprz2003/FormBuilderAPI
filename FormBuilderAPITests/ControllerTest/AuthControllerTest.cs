using System;
using System.Threading.Tasks;
using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Newtonsoft.Json;

namespace FormBuilderAPITests.ControllerTest
{
    public class AuthControllerTest
    {
        private readonly Mock<IAuthBL> _mockAuthBL;
        private readonly AuthController _controller;

        public AuthControllerTest()
        {
            _mockAuthBL = new Mock<IAuthBL>();
            _controller = new AuthController(_mockAuthBL.Object);
        }

        #region Login Tests

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "testuser",
                Password = "password123"
            };

            var authResponse = new AuthResponseDTO
            {
                UserId = "1",
                Username = "testuser",
                Role = "Learner",
                Token = "jwt-token-here",
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };

            _mockAuthBL.Setup(bl => bl.LoginAsync(It.IsAny<AuthDTO>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Convert to anonymous type to access properties
            var responseJson = JsonConvert.SerializeObject(okResult.Value);
            var responseObj = JsonConvert.DeserializeAnonymousType(responseJson, new { 
                message = "", 
                data = new {
                    UserId = "",
                    Username = "",
                    Role = "",
                    Token = ""
                }
            });

            Assert.Equal("Login successful.", responseObj.message);
            Assert.NotNull(responseObj.data);
            Assert.Equal(authResponse.UserId, responseObj.data.UserId);
            Assert.Equal(authResponse.Username, responseObj.data.Username);
            Assert.Equal(authResponse.Role, responseObj.data.Role);
            Assert.Equal(authResponse.Token, responseObj.data.Token);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                Username = "wronguser",
                Password = "wrongpass"
            };

            _mockAuthBL.Setup(bl => bl.LoginAsync(It.IsAny<AuthDTO>()))
                .ReturnsAsync((AuthResponseDTO)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);
            
            var responseJson = JsonConvert.SerializeObject(unauthorizedResult.Value);
            var responseObj = JsonConvert.DeserializeAnonymousType(responseJson, new { message = "" });
            
            Assert.Equal("Invalid username or password.", responseObj.message);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new AuthDTO
            {
                // Missing required fields
            };

            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_ValidLearner_ReturnsCreatedWithToken()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "newuser",
                Password = "password123",
                Role = "Learner"
            };

            var authResponse = new AuthResponseDTO
            {
                UserId = "2",
                Username = "newuser",
                Role = "Learner",
                Token = "jwt-token-here",
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };

            _mockAuthBL.Setup(bl => bl.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            _mockAuthBL.Setup(bl => bl.RegisterAsync(It.IsAny<AuthDTO>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(AuthController.Login), createdResult.ActionName);
            Assert.Equal(authResponse.UserId, createdResult.RouteValues["id"]);
            
            Assert.NotNull(createdResult.Value);
            
            var responseJson = JsonConvert.SerializeObject(createdResult.Value);
            var responseObj = JsonConvert.DeserializeAnonymousType(responseJson, new { 
                message = "", 
                data = new {
                    UserId = "",
                    Username = "",
                    Role = "",
                    Token = ""
                }
            });
            
            Assert.Equal("Registration successful. You can now log in.", responseObj.message);
            Assert.NotNull(responseObj.data);
            Assert.Equal(authResponse.UserId, responseObj.data.UserId);
            Assert.Equal(authResponse.Username, responseObj.data.Username);
            Assert.Equal(authResponse.Role, responseObj.data.Role);
            Assert.Equal(authResponse.Token, responseObj.data.Token);
        }

        [Fact]
        public async Task Register_AttemptAdminRegistration_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "adminuser",
                Password = "password123",
                Role = "Admin"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            
            var responseJson = JsonConvert.SerializeObject(badRequestResult.Value);
            var responseObj = JsonConvert.DeserializeAnonymousType(responseJson, new { message = "" });
            
            Assert.Equal("Admin accounts cannot be registered via API. Please contact the system administrator.", 
                responseObj.message);
        }

        [Fact]
        public async Task Register_UsernameAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "existinguser",
                Password = "password123"
            };

            var existingUser = new User
            {
                UserId = 3,
                Username = "existinguser"
            };

            _mockAuthBL.Setup(bl => bl.GetUserByUsernameAsync(registerDto.Username))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
            
            var responseJson = JsonConvert.SerializeObject(conflictResult.Value);
            var responseObj = JsonConvert.DeserializeAnonymousType(responseJson, new { message = "" });
            
            Assert.Equal("User already exists. Please log in instead.", 
                responseObj.message);
        }

        [Fact]
        public async Task Register_RegistrationFailed_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                Username = "newuser",
                Password = "password123"
            };

            _mockAuthBL.Setup(bl => bl.GetUserByUsernameAsync(registerDto.Username))
                .ReturnsAsync((User)null);

            _mockAuthBL.Setup(bl => bl.RegisterAsync(It.IsAny<AuthDTO>()))
                .ReturnsAsync((AuthResponseDTO)null);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            
            var responseJson = JsonConvert.SerializeObject(badRequestResult.Value);
            var responseObj = JsonConvert.DeserializeAnonymousType(responseJson, new { message = "" });
            
            Assert.Equal("Registration failed. Please try again later.", 
                responseObj.message);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthDTO
            {
                // Missing required fields
            };

            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}
