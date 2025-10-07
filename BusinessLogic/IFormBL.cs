using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public interface IFormBL
    {
        // Get all forms (Admin & Learner)
        Task<IEnumerable<Form>> GetAllFormsAsync();

        // Get specific form by Id (Admin & Learner)
        Task<Form?> GetFormByIdAsync(string id);

        // Create a new form (Admin only)
        Task<string> CreateFormAsync(FormDTO dto);

        // Update an existing form (Admin only)
        Task<bool> UpdateFormAsync(string id, FormDTO dto);

        // Delete a form by Id (Admin only)
        Task<bool> DeleteFormAsync(string id);
    }
}
