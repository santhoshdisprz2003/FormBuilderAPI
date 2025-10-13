using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IFormBL
    {
        Task<IEnumerable<Form>> GetAllFormsAsync(string userRole);
        Task<Form?> GetFormByIdAsync(string id, string userRole);

        Task<bool> CreateFormLayoutAsync(FormLayoutDTO layoutDto);
        Task<string> CreateFormConfigAsync(FormConfigDTO configDto);
        Task<bool> UpdateFormAsync(string id, FormDTO dto);
        Task<bool> DeleteFormAsync(string id);
        Task<Form> PublishFormAsync(string id);
    }
}
