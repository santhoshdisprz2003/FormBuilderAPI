using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IEnumerable<Form>> GetAllFormsAsync(string userRole)
        {
            if (userRole == "Admin")
            {
                // Admin sees everything
                return await _forms.Find(_ => true).ToListAsync();
            }
            else
            {
                // Learners see only Published forms
                return await _forms.Find(f => f.Status == FormStatus.Published).ToListAsync();
            }
        }

        public async Task<Form?> GetFormByIdAsync(string id, string userRole)
        {
            var filter = Builders<Form>.Filter.Eq(f => f.Id, id);

            if (userRole != "Admin")
            {
                // Learners can access only published forms
                var statusFilter = Builders<Form>.Filter.Eq(f => f.Status, FormStatus.Published);
                filter = Builders<Form>.Filter.And(filter, statusFilter);
            }

            return await _forms.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<string> CreateFormConfigAsync(FormConfigDTO dto)
        {
            var form = new Form
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = FormStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                Sections = new List<FormSection>() // empty initially
            };

            await _forms.InsertOneAsync(form);
            return form.Id;
        }

        public async Task<bool> CreateFormLayoutAsync(FormLayoutDTO layoutDto)
        {
            var existingForm = await _forms.Find(f => f.Id == layoutDto.FormId).FirstOrDefaultAsync();
            if (existingForm == null)
                throw new Exception("Form not found.");

            // Map DTO sections to Mongo sections
            var sections = layoutDto.Sections?.ConvertAll(s => new FormSection
            {
                Title = s.Title,
                Fields = s.Fields?.ConvertAll(f => new FormField
                {
                    Label = f.Label,
                    Type = f.Type,
                    Required = f.Required,
                    Options = f.Options ?? new List<string>()
                }) ?? new List<FormField>()
            }) ?? new List<FormSection>();

            var update = Builders<Form>.Update.Set(f => f.Sections, sections);

            var result = await _forms.UpdateOneAsync(f => f.Id == layoutDto.FormId, update);

            return result.ModifiedCount > 0;
        }


        public async Task<bool> UpdateFormAsync(string id, FormDTO dto)
        {
            var existing = await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return false;

            if (existing.Status == FormStatus.Published)
                return false;  // Cannot update published forms

            var update = Builders<Form>.Update
                .Set(f => f.Title, dto.Title)
                .Set(f => f.Description, dto.Description)
                .Set(f => f.Sections, dto.Sections?.ConvertAll(s => new FormSection
                {
                    Title = s.Title,
                    Fields = s.Fields?.ConvertAll(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        Required = f.Required,
                        Options = f.Options ?? new List<string>()
                    }) ?? new List<FormField>()
                }) ?? new List<FormSection>())
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _forms.UpdateOneAsync(f => f.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<Form> PublishFormAsync(string id)
        {
            var existing = await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                throw new Exception("Form not found.");

            if (existing.Status == FormStatus.Published)
                throw new InvalidOperationException("Form is already published.");

            var update = Builders<Form>.Update
                .Set(f => f.Status, FormStatus.Published)
                .Set(f => f.PublishedAt, DateTime.UtcNow);

            await _forms.UpdateOneAsync(f => f.Id == id, update);

            // Return the updated form
            return await _forms.Find(f => f.Id == id).FirstOrDefaultAsync()!;
        }

        public async Task<bool> DeleteFormAsync(string id)
        {
            // Delete all responses from SQL first
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

            // Delete form from MongoDB
            var result = await _forms.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
