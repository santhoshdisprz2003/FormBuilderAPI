using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.MongoModel;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormBuilderAPI.Repository
{
    public class FormRepository : IFormRepository
    {
        private readonly IMongoCollection<Form> _forms;
        private readonly SQLDbContext _sqlContext;

        public FormRepository(MongoDbContext mongoContext, SQLDbContext sqlContext)
        {
            _forms = mongoContext.Forms;
            _sqlContext = sqlContext;
        }

        public async Task<(IEnumerable<Form> Forms, long TotalCount)> GetAllFormsAsync(
            string userRole,
            int pageNumber,
            int pageSize,
            string? search = null)
        {
            var filterBuilder = Builders<Form>.Filter;
            var filter = filterBuilder.Empty;

            if (userRole != "Admin")
                filter = filterBuilder.Eq(f => f.Status, FormStatus.Published);

            if (!string.IsNullOrEmpty(search))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex("Config.Title", new MongoDB.Bson.BsonRegularExpression(search, "i")),
                    filterBuilder.Regex("Config.Description", new MongoDB.Bson.BsonRegularExpression(search, "i"))
                );
                filter = filterBuilder.And(filter, searchFilter);
            }

            int offset = (pageNumber - 1) * pageSize;
            var totalCount = await _forms.CountDocumentsAsync(filter);

            var forms = await _forms.Find(filter)
                                    .SortByDescending(f => f.CreatedAt)
                                    .Skip(offset)
                                    .Limit(pageSize)
                                    .ToListAsync();

            return (forms, totalCount);
        }

        public async Task<Form?> GetFormByIdAsync(string id, string userRole)
        {
            var filter = Builders<Form>.Filter.Eq(f => f.Id, id);

            if (userRole != "Admin")
            {
                var statusFilter = Builders<Form>.Filter.Eq(f => f.Status, FormStatus.Published);
                filter = Builders<Form>.Filter.And(filter, statusFilter);
            }

            return await _forms.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<Form?> GetFormByIdAsync(string id)
        {
            return await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
        }

        public async Task<string> InsertFormAsync(Form form)
        {
            await _forms.InsertOneAsync(form);
            return form.Id;
        }

        public async Task<bool> UpdateFormConfigAsync(string id, string title, string description)
        {
            var update = Builders<Form>.Update
                .Set(f => f.Config.Title, title)
                .Set(f => f.Config.Description, description)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _forms.UpdateOneAsync(f => f.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateFormLayoutAsync(string formId, FormLayout layout)
        {
            var update = Builders<Form>.Update
                .Set(f => f.Layout, layout)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _forms.UpdateOneAsync(f => f.Id == formId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateFormStatusAsync(string id, FormStatus status, DateTime publishedAt)
        {
            var update = Builders<Form>.Update
                .Set(f => f.Status, status)
                .Set(f => f.PublishedAt, publishedAt);

            var result = await _forms.UpdateOneAsync(f => f.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteFormAsync(string id)
        {
            var result = await _forms.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> DeleteFormResponsesAsync(string formId)
        {
            var responses = await _sqlContext.FormResponses
                .Where(r => r.FormId == formId)
                .Include(r => r.Answers)
                .ToListAsync();

            if (responses.Any())
            {
                var allAnswers = responses.SelectMany(r => r.Answers).ToList();
                if (allAnswers.Any())
                    _sqlContext.FormResponseAnswers.RemoveRange(allAnswers);

                _sqlContext.FormResponses.RemoveRange(responses);
                await _sqlContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}