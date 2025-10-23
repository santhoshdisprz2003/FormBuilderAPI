using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoFormStatus = FormBuilderAPI.Model.MongoModel.FormStatus;
using DTOFormStatus = FormBuilderAPI.DTOs.FormStatus;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class FormBL : IFormBL
    {
        private readonly IMongoCollection<Form> _forms;
        private readonly SQLDbContext _sqlContext;

        public FormBL(MongoDbContext mongoContext, SQLDbContext sqlContext)
        {
            _forms = mongoContext.Forms;
            _sqlContext = sqlContext;
        }

        public async Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(string userRole, int offset, int limit)
        {
            var filter = Builders<Form>.Filter.Empty;

            // Apply filter for non-admin users
            if (userRole != "Admin")
                filter = Builders<Form>.Filter.Eq(f => f.Status, MongoFormStatus.Published);

            // Get total count for pagination info
            var totalCount = await _forms.CountDocumentsAsync(filter);

            // Apply pagination
            var forms = await _forms.Find(filter)
                                    .SortByDescending(f => f.CreatedAt)  // optional, helps with ordering
                                    .Skip(offset)
                                    .Limit(limit)
                                    .ToListAsync();

            return (forms, totalCount);
        }

        public async Task<Form?> GetFormByIdAsync(string id, string userRole)
        {
            var filter = Builders<Form>.Filter.Eq(f => f.Id, id);

            if (userRole != "Admin")
            {
                var statusFilter = Builders<Form>.Filter.Eq(f => f.Status, MongoFormStatus.Published);
                filter = Builders<Form>.Filter.And(filter, statusFilter);
            }

            return await _forms.Find(filter).FirstOrDefaultAsync();
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
                Layout = new FormLayout(), // Empty layout initially
                Status = MongoFormStatus.Draft,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            await _forms.InsertOneAsync(form);
            return form.Id;
        }

        public async Task<bool> UpdateFormAsync(string id, FormDTO dto)
        {
            var existing = await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == MongoFormStatus.Published)
                throw new InvalidOperationException("Cannot edit a published form.");

            // Build Layout object (from CreateFormLayoutAsync logic)
            var updatedLayout = new FormLayout
            {
                HeaderCard = new FormHeaderCard
                {
                    Title = dto.Layout.HeaderCard.Title,
                    Description = dto.Layout.HeaderCard.Description
                    // MongoDB generates _id automatically
                },
                Fields = dto.Layout.Fields?.Select(f => new FormField
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
                        // OptionId auto-generated
                    }).ToList() ?? new List<FieldOption>(),
                    Format = f.Format,
                    Required = f.Required,
                    Order = f.Order
                    // QuestionId auto-generated
                }).ToList() ?? new List<FormField>()
            };

            // Build update definition (combine Config + Layout updates)
            var update = Builders<Form>.Update
                .Set(f => f.Config.Title, dto.Config.Title)
                .Set(f => f.Config.Description, dto.Config.Description)
                .Set(f => f.Layout, updatedLayout)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _forms.UpdateOneAsync(f => f.Id == id, update);
            return result.ModifiedCount > 0;
        }


        public async Task<Form> PublishFormAsync(string id)
        {
            var existing = await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == MongoFormStatus.Published)
                throw new InvalidOperationException("Form is already published.");

            var update = Builders<Form>.Update
                .Set(f => f.Status, MongoFormStatus.Published)
                .Set(f => f.PublishedAt, DateTime.UtcNow);

            await _forms.UpdateOneAsync(f => f.Id == id, update);

            return await _forms.Find(f => f.Id == id).FirstOrDefaultAsync()!;
        }

        public async Task<bool> DeleteFormAsync(string id)
        {
            // Delete related responses from SQL first
            var responses = await _sqlContext.FormResponses
                .Where(r => r.FormId == id)
                .Include(r => r.Answers)
                .ToListAsync();

            if (responses.Any())
            {
                var allAnswers = responses.SelectMany(r => r.Answers).ToList();
                if (allAnswers.Any())
                    _sqlContext.FormResponseAnswers.RemoveRange(allAnswers);

                _sqlContext.FormResponses.RemoveRange(responses);
                await _sqlContext.SaveChangesAsync();
            }

            var result = await _forms.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
