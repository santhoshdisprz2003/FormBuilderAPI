using FormBuilderAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IResponseBL
    {
        Task<List<ResponseDetailDTO>> GetAllResponsesAsync();
        Task<List<ResponseDetailDTO>> GetResponsesForFormAsync(string formId);
        Task<ResponseDetailDTO?> GetResponseByIdAsync(Guid id);
        Task<Guid> SubmitResponseAsync(ResponseDTO dto);
        Task<bool> DeleteResponseAsync(Guid id);
    }
}
