using FormBuilderAPI.Model.SQLModel;
using System.Threading.Tasks;

namespace FormBuilderAPI.Repository
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User> InsertUserAsync(User user);
        Task<bool> UserExistsAsync(string username);
        Task SaveChangesAsync();
    }
}
