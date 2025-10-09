using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.Model.SQLModel; // ✅ Add this line for EF models
using Microsoft.EntityFrameworkCore; 
using System.Linq;
namespace FormBuilderAPI.BusinessLogicLayer
{
    public class FormBL : IFormBL
    {
        private readonly IMongoCollection<Form> _forms;
        private readonly SQLDbContext _sqlContext;

        public FormBL(MongoDbContext mongoContext,SQLDbContext sqlContext)
        {
            _forms = mongoContext.Forms;
            _sqlContext = sqlContext;
        }

        /// <summary>
        /// Get all forms (Admin & Learner)
        /// </summary>
        public async Task<IEnumerable<Form>> GetAllFormsAsync()
        {
            return await _forms.Find(_ => true).ToListAsync();
        }

        /// <summary>
        /// Get specific form by Id (Admin & Learner)
        /// </summary>
        public async Task<Form?> GetFormByIdAsync(string id)
        {
            return await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Create a new form (Admin only)
        /// </summary>
        public async Task<string> CreateFormAsync(FormDTO dto)
        {
            var form = new Form
            {
                Title = dto.Title,
                Description = dto.Description,
                Sections = dto.Sections?.ConvertAll(s => new FormSection
                {
                    Title = s.Title,
                    Fields = s.Fields?.ConvertAll(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        Required = f.Required,
                        Options = f.Options ?? new List<string>()
                    }) ?? new List<FormField>()
                }) ?? new List<FormSection>()
            };

            await _forms.InsertOneAsync(form);
            return form.Id; // Return newly created form ID
        }

        /// <summary>
        /// Update existing form (Admin only)
        /// </summary>
        public async Task<bool> UpdateFormAsync(string id, FormDTO dto)
        {
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
                }) ?? new List<FormSection>());

            var result = await _forms.UpdateOneAsync(f => f.Id == id, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Delete a form by Id (Admin only)
        /// </summary>
        public async Task<bool> DeleteFormAsync(string id)
{
    // 1️⃣ Delete all responses for this form from SQL
    var responses = await _sqlContext.FormResponses
        .Where(r => r.FormId == id)
        .Include(r => r.Answers)
        .ToListAsync();

    if (responses.Any())
    {
        // Delete all answers first
        var allAnswers = responses.SelectMany(r => r.Answers).ToList();
        if (allAnswers.Any())
            _sqlContext.FormResponseAnswers.RemoveRange(allAnswers);

        // Then delete all responses
        _sqlContext.FormResponses.RemoveRange(responses);

        await _sqlContext.SaveChangesAsync();
    }

    // 2️⃣ Delete the form from MongoDB
    var result = await _forms.DeleteOneAsync(f => f.Id == id);

    // 3️⃣ Return whether form deletion was successful
    return result.DeletedCount > 0;
}

    }
}
