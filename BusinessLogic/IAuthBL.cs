using System.Threading.Tasks;
using FormBuilderAPI.DTOs;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IAuthBL
    {
        Task<AuthResponseDTO?> LoginAsync(AuthDTO dto);
        Task<AuthResponseDTO?> RegisterAsync(AuthDTO dto);
        Task<bool> ValidateTokenAsync(string token);
    }
}
