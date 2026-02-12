using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FormBuilderAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly SQLDbContext _sqlContext;

        public UserRepository(SQLDbContext sqlContext)
        {
            _sqlContext = sqlContext;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _sqlContext.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _sqlContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User> InsertUserAsync(User user)
        {
            _sqlContext.Users.Add(user);
            await _sqlContext.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await _sqlContext.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task SaveChangesAsync()
        {
            await _sqlContext.SaveChangesAsync();
        }
    }
}
