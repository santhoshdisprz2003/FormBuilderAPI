using FormBuilderAPI.Model.MongoModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.Repository
{
public interface IFormRepository
{
    Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(
        string userRole,
        int pageNumber,
        int pageSize,
        string? search = null);

    Task<Form?> GetFormByIdAsync(string id, string userRole);
    Task<Form?> GetFormByIdAsync(string id);
    Task<string> InsertFormAsync(Form form);
    Task<bool> UpdateFormConfigAsync(string id, string title, string description);
    Task<bool> UpdateFormLayoutAsync(string formId, FormLayout layout);
    Task<bool> UpdateFormStatusAsync(string id, FormStatus status, DateTime publishedAt);
    Task<bool> DeleteFormAsync(string id);
    Task<bool> DeleteFormResponsesAsync(string formId);
}
}