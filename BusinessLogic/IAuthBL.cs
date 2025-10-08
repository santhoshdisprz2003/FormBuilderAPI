using System.Threading.Tasks;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.SQLModel;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IAuthBL
    {
        Task<AuthResponseDTO?> LoginAsync(AuthDTO dto);
        Task<AuthResponseDTO?> RegisterAsync(AuthDTO dto);
        Task<bool> ValidateTokenAsync(string token);
        Task<User?> GetUserByUsernameAsync(string username);
        

    }
}
