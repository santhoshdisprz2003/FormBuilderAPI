using FormBuilderAPI.Model.SQLModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.Repository
{
    public interface IResponseRepository
    {
        // FormResponse operations
        Task<int> InsertResponseAsync(FormResponse response);
        Task<List<FormResponse>> GetResponsesByFormIdAsync(string formId);
        Task<List<FormResponse>> GetResponsesByUserIdAsync(string userId);
        Task<List<FormResponse>> GetResponsesByFormIdAndUserIdAsync(string formId, string userId);

        // ResponseFile operations
        Task InsertResponseFileAsync(ResponseFile file);
        Task<List<ResponseFile>> GetFilesByResponseIdAsync(int responseId);
        Task<ResponseFile?> GetFileByResponseIdAndFileIdAsync(int responseId, int fileId);

        // User operations
        Task<Dictionary<string, string>> GetUserNamesByIdsAsync(List<string> userIds);

        // Bulk operations
        Task SaveChangesAsync();
    }
}
