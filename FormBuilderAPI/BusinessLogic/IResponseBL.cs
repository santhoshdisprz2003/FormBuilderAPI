using FormBuilderAPI.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IResponseBL
    {

        Task<int> SubmitResponseAsync(ResponseDTO dto);
        Task<List<ResponseDetailDTO>> GetResponsesForUserAsync(string formId, string userId);
        Task<object> GetResponsesForFormAsync(string formId, string? search = null, int pageNumber = 1, int pageSize = 6);
       Task<object> GetAllResponsesByUserAsync(string userId, string? search = null, int pageNumber = 1, int pageSize = 6);

        Task<ResponseFileDTO?> GetFileByResponseIdAndFileIdAsync(int responseId, int fileId);

    }
}
