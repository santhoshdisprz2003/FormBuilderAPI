using System;
using System.Threading.Tasks;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Helper;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Repository;
using Microsoft.Extensions.Configuration;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class AuthBL : IAuthBL
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly PasswordHasher _passwordHasher;

        // Token expiration constant
        private const int TOKEN_EXPIRATION_HOURS = 2;

        public AuthBL(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _jwtHelper = new JwtHelper(config);
            _passwordHasher = new PasswordHasher();
        }

        public async Task<AuthResponseDTO?> LoginAsync(AuthDTO dto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return null;

            // Get user from repository
            var user = await _userRepository.GetUserByUsernameAsync(dto.Username);
            if (user == null)
                return null;

            // Verify password
            if (!VerifyPassword(dto.Password, user.PasswordHash))
                return null;

            // Generate token and return response
            return CreateAuthResponse(user);
        }

        public async Task<AuthResponseDTO?> RegisterAsync(AuthDTO dto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return null;

            // Check if user already exists
            var userExists = await _userRepository.UserExistsAsync(dto.Username);
            if (userExists)
                return null;

            // Create new user
            var newUser = CreateNewUser(dto);

            // Hash password
            newUser.PasswordHash = HashPassword(dto.Password);

            // Save user to repository
            var savedUser = await _userRepository.InsertUserAsync(newUser);

            // Generate token and return response
            return CreateAuthResponse(savedUser);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            // Token validation logic can be implemented here
            // For now, returning true as placeholder
            return await Task.FromResult(true);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _userRepository.GetUserByUsernameAsync(username);
        }

        #region Private Helper Methods

        private User CreateNewUser(AuthDTO dto)
        {
            return new User
            {
                Username = dto.Username,
                Role = DetermineUserRole(dto.Role),
                CreatedAt = DateTime.UtcNow
            };
        }

        private string DetermineUserRole(string? requestedRole)
        {
            // Default to "Learner" if no role specified or invalid role
            if (string.IsNullOrWhiteSpace(requestedRole))
                return "Learner";

            // Validate role (only Admin or Learner allowed)
            return requestedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
                ? "Admin" 
                : "Learner";
        }

        private string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(password);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return _passwordHasher.VerifyPassword(password, passwordHash);
        }

        private AuthResponseDTO CreateAuthResponse(User user)
        {
            var token = GenerateToken(user);
            var expirationTime = CalculateTokenExpiration();

            return new AuthResponseDTO
            {
                UserId = user.UserId.ToString(),
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = expirationTime
            };
        }

        private string GenerateToken(User user)
        {
            return _jwtHelper.GenerateToken(
                user.UserId.ToString(), 
                user.Username, 
                user.Role
            );
        }

        private DateTime CalculateTokenExpiration()
        {
            return DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS);
        }

        #endregion
    }
}