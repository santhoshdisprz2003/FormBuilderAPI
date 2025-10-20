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
        Task<string> CreateFormConfigAsync(FormConfigDTO configDto,string createdBy);
        Task<bool> CreateFormLayoutAsync(string formId, FormLayoutDTO layoutDto);
        Task<bool> UpdateFormAsync(string id, FormDTO formDto);
        Task<bool> DeleteFormAsync(string id);
        Task<Form> PublishFormAsync(string id);
    }
}
