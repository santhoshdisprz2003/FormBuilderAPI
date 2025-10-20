using FormBuilderAPI.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace FormBuilderAPITests.HelperTest
{
    public class JwtHelperTest
    {
        private readonly IConfiguration _configuration;
        private readonly JwtHelper _jwtHelper;
        private readonly string _testSecret = "ThisIsAVeryLongTestSecretKeyForJwtTokenGeneration1234567890";
        private readonly string _testIssuer = "test-issuer";
        private readonly string _testAudience = "test-audience";
        private readonly int _testExpiryMinutes = 60;

        public JwtHelperTest()
        {
            // Create a real configuration object with test values
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", _testSecret},
                {"Jwt:Issuer", _testIssuer},
                {"Jwt:Audience", _testAudience},
                {"Jwt:ExpireMinutes", _testExpiryMinutes.ToString()}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _jwtHelper = new JwtHelper(_configuration);
        }

        [Fact]
        public void GenerateToken_ValidInputs_ReturnsValidToken()
        {
            // Arrange
            string userId = "123";
            string username = "testuser";
            string role = "Admin";

            // Act
            string token = _jwtHelper.GenerateToken(userId, username, role);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // Decode the token to verify its contents
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Verify claims
            Assert.Equal(userId, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal(username, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
            Assert.Equal(role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
            
            // Verify token properties
            Assert.Equal(_testIssuer, jwtToken.Issuer);
            Assert.Equal(_testAudience, jwtToken.Audiences.First());
            
            // Verify expiration (should be approximately _testExpiryMinutes from now)
            var expectedExpiry = DateTime.UtcNow.AddMinutes(_testExpiryMinutes);
            var actualExpiry = jwtToken.ValidTo;
            
            // Allow a small time difference (5 seconds) due to test execution time
            Assert.True(Math.Abs((expectedExpiry - actualExpiry).TotalSeconds) < 5);
        }

        [Fact]
        public void GetValidationParameters_ReturnsCorrectParameters()
        {
            // Act
            var validationParameters = _jwtHelper.GetValidationParameters();

            // Assert
            Assert.NotNull(validationParameters);
            Assert.True(validationParameters.ValidateIssuer);
            Assert.True(validationParameters.ValidateAudience);
            Assert.True(validationParameters.ValidateIssuerSigningKey);
            Assert.Equal(_testIssuer, validationParameters.ValidIssuer);
            Assert.Equal(_testAudience, validationParameters.ValidAudience);
            Assert.Equal(TimeSpan.Zero, validationParameters.ClockSkew);

            // Verify signing key
            var expectedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_testSecret));
            var actualKey = validationParameters.IssuerSigningKey as SymmetricSecurityKey;
            
            Assert.NotNull(actualKey);
            Assert.Equal(expectedKey.KeySize, actualKey.KeySize);
            Assert.Equal(Convert.ToBase64String(expectedKey.Key), Convert.ToBase64String(actualKey.Key));
        }

        [Fact]
        public void GenerateAndValidateToken_TokenIsValid()
        {
            // Arrange
            string userId = "123";
            string username = "testuser";
            string role = "Admin";

            // Act
            string token = _jwtHelper.GenerateToken(userId, username, role);
            var validationParameters = _jwtHelper.GetValidationParameters();
            
            // Assert - Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);
            
            // Verify the token was validated successfully
            Assert.NotNull(principal);
            Assert.NotNull(securityToken);
            Assert.IsType<JwtSecurityToken>(securityToken);
            
            // Directly examine the JWT token to verify claims
            var jwtToken = (JwtSecurityToken)securityToken;
            
            // Verify claims using the token directly
            Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == username);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == role);
        }

        [Fact]
        public void GenerateToken_MissingSecretKey_ThrowsException()
        {
            // Arrange - Create configuration with missing key
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Issuer", _testIssuer},
                {"Jwt:Audience", _testAudience},
                {"Jwt:ExpireMinutes", _testExpiryMinutes.ToString()}
                // Key is missing
            };

            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var jwtHelper = new JwtHelper(badConfig);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                jwtHelper.GenerateToken("123", "testuser", "Admin"));
                
            Assert.Contains("JWT secret key is not configured", exception.Message);
        }
    }
}
