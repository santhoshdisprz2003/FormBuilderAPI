using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IFormBL
    {
        Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(
            string userRole,
            int pageNumber,
            int pageSize,
            string? search = null);

        Task<Form?> GetFormByIdAsync(string id, string userRole);
        Task<string> CreateFormConfigAsync(FormConfigDTO configDto, string createdBy);
        Task<bool> UpdateFormConfigAsync(string id, FormConfigDTO configDto);
        Task<bool> CreateFormLayoutAsync(string formId, FormLayoutDTO layoutDto);
        Task<bool> UpdateFormLayoutAsync(string formId, FormLayoutDTO layoutDto);
        Task<bool> DeleteFormAsync(string id);
        Task<Form> PublishFormAsync(string id);
    }
}
