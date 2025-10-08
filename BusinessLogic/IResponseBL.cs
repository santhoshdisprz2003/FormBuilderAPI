using FormBuilderAPI.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IResponseBL
    {
    
        Task<Guid> SubmitResponseAsync(ResponseDTO dto);
        Task<List<ResponseDetailDTO>> GetResponsesForFormAsync(string formId);
    }
}
