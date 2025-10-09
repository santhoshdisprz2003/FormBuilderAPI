using System;
using System.Linq;
using System.Threading.Tasks;
using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Helper;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class AuthBL : IAuthBL
    {
        private readonly SQLDbContext _sqlContext;
        private readonly JwtHelper _jwtHelper;
        private readonly PasswordHasher _passwordHasher;

        public AuthBL(SQLDbContext sqlContext, IConfiguration config)
        {
            _sqlContext = sqlContext;
            _jwtHelper = new JwtHelper(config);
            _passwordHasher = new PasswordHasher();
        }

        // LOGIN
        public async Task<AuthResponseDTO?> LoginAsync(AuthDTO dto)
        {
            var user = await _sqlContext.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null)
                return null;

            if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                return null;

            var token = _jwtHelper.GenerateToken(user.UserId.ToString(), user.Username, user.Role);

            return new AuthResponseDTO
            {
                UserId = user.UserId.ToString(),
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }

        // ✅ REGISTER
        public async Task<AuthResponseDTO?> RegisterAsync(AuthDTO dto)
        {
            var existingUser = await _sqlContext.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (existingUser != null)
                return null;

            var hashed = _passwordHasher.HashPassword(dto.Password);
            var newUser = new User
            {
                Username = dto.Username,
                PasswordHash = hashed,
                Role = dto.Role ?? "Learner"
            };

            _sqlContext.Users.Add(newUser);
            await _sqlContext.SaveChangesAsync();

            var token = _jwtHelper.GenerateToken(newUser.UserId.ToString(), newUser.Username, newUser.Role);

            return new AuthResponseDTO
            {
                UserId = newUser.UserId.ToString(),
                Username = newUser.Username,
                Role = newUser.Role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }

        // ✅ Token validation placeholder
        public async Task<bool> ValidateTokenAsync(string token)
        {
            return await Task.FromResult(true);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _sqlContext.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }


    }
}
