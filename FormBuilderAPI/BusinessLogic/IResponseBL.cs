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
        Task<List<ResponseDetailDTO>> GetResponsesForFormAsync(string formId);
          Task<List<ResponseDetailDTO>> GetAllResponsesByUserAsync(string userId);


Task<ResponseFileDTO?> GetFileByResponseIdAndFileIdAsync(int responseId, int fileId);

     

    }
}
