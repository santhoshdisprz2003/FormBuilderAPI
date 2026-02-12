using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using FormBuilderAPI.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Add alias to resolve ambiguity
using MongoFormStatus = FormBuilderAPI.Model.MongoModel.FormStatus;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class FormBL : IFormBL
    {
        private readonly IFormRepository _formRepository;

        public FormBL(IFormRepository formRepository)
        {
            _formRepository = formRepository;
        }

        public async Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(
            string userRole,
            int pageNumber,
            int pageSize,
            string? search = null)
        {
            return await _formRepository.GetAllFormsAsync(userRole, pageNumber, pageSize, search);
        }

        public async Task<Form?> GetFormByIdAsync(string id, string userRole)
        {
            return await _formRepository.GetFormByIdAsync(id, userRole);
        }

        public async Task<string> CreateFormConfigAsync(FormConfigDTO dto, string createdBy)
        {
            var form = new Form
            {
                Config = new FormConfig
                {
                    Title = dto.Title,
                    Description = dto.Description
                },
                Layout = new FormLayout(),
                Status = MongoFormStatus.Draft,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            return await _formRepository.InsertFormAsync(form);
        }

        public async Task<bool> UpdateFormConfigAsync(string id, FormConfigDTO dto)
        {
            var existing = await _formRepository.GetFormByIdAsync(id);
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == MongoFormStatus.Published)
                throw new InvalidOperationException("Cannot edit a published form.");

            return await _formRepository.UpdateFormConfigAsync(id, dto.Title, dto.Description);
        }

        public async Task<bool> CreateFormLayoutAsync(string formId, FormLayoutDTO dto)
        {
            var existing = await _formRepository.GetFormByIdAsync(formId);
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == MongoFormStatus.Published)
                throw new InvalidOperationException("Cannot add layout to a published form.");

            var layout = MapDtoToLayout(dto);
            return await _formRepository.UpdateFormLayoutAsync(formId, layout);
        }

        public async Task<bool> UpdateFormLayoutAsync(string formId, FormLayoutDTO dto)
        {
            var existing = await _formRepository.GetFormByIdAsync(formId);
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == MongoFormStatus.Published)
                throw new InvalidOperationException("Cannot edit a published form.");

            var updatedLayout = MapDtoToLayout(dto);
            return await _formRepository.UpdateFormLayoutAsync(formId, updatedLayout);
        }

        public async Task<Form> PublishFormAsync(string id)
        {
            var existing = await _formRepository.GetFormByIdAsync(id);
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == MongoFormStatus.Published)
                throw new InvalidOperationException("Form is already published.");

            await _formRepository.UpdateFormStatusAsync(id, MongoFormStatus.Published, DateTime.UtcNow);

            return await _formRepository.GetFormByIdAsync(id) ?? throw new Exception("Failed to retrieve published form.");
        }

        public async Task<bool> DeleteFormAsync(string id)
        {
            await _formRepository.DeleteFormResponsesAsync(id);
            return await _formRepository.DeleteFormAsync(id);
        }

        #region Private Helper Methods

        private FormLayout MapDtoToLayout(FormLayoutDTO dto)
        {
            return new FormLayout
            {
                HeaderCard = new FormHeaderCard
                {
                    Title = dto.HeaderCard.Title,
                    Description = dto.HeaderCard.Description
                },
                Fields = dto.Fields?.Select(f => new FormField
                {
                    Label = f.Label,
                    Type = f.Type,
                    DescriptionEnabled = f.DescriptionEnabled,
                    Description = f.Description,
                    SingleChoice = f.SingleChoice,
                    MultipleChoice = f.MultipleChoice,
                    Options = f.Options?.Select(o => new FieldOption
                    {
                        Value = o.Value
                    }).ToList() ?? new List<FieldOption>(),
                    Format = f.Format,
                    Required = f.Required,
                    Order = f.Order
                }).ToList() ?? new List<FormField>()
            };
        }

        #endregion
    }
}