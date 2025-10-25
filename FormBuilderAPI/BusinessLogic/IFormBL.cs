using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IFormBL
    {
        Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(string userRole, int offset, int limit);
        Task<Form?> GetFormByIdAsync(string id, string userRole);

        // Config
        Task<string> CreateFormConfigAsync(FormConfigDTO configDto, string createdBy);
        Task<bool> UpdateFormConfigAsync(string id, FormConfigDTO configDto);

        // Layout
        Task<bool> CreateFormLayoutAsync(string formId, FormLayoutDTO layoutDto);
        Task<bool> UpdateFormLayoutAsync(string formId, FormLayoutDTO layoutDto);

        // Other operations
        Task<bool> DeleteFormAsync(string id);
        Task<Form> PublishFormAsync(string id);
    }
}
