using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.MongoModel;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class FormBL : IFormBL
    {
        private readonly IMongoCollection<Form> _forms;

        public FormBL(MongoDbContext mongoContext)
        {
            _forms = mongoContext.Forms;
        }

        public async Task<IEnumerable<Form>> GetAllFormsAsync()
        {
            return await _forms.Find(_ => true).ToListAsync();
        }

        public async Task<Form?> GetFormByIdAsync(string id)
        {
            return await _forms.Find(f => f.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Form> CreateFormAsync(FormDTO dto)
        {
            var form = new Form
            {
                Title = dto.Title,
                Description = dto.Description,
                Sections = dto.Sections.ConvertAll(s => new FormSection
                {
                    Title = s.Title,
                    Fields = s.Fields.ConvertAll(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        Required = f.Required,
                        Options = f.Options ?? new List<string>()
                    })
                })
            };

            await _forms.InsertOneAsync(form);
            return form;
        }

        public async Task<bool> UpdateFormAsync(string id, FormDTO dto)
        {
            var update = Builders<Form>.Update
                .Set(f => f.Title, dto.Title)
                .Set(f => f.Description, dto.Description)
                .Set(f => f.Sections, dto.Sections.ConvertAll(s => new FormSection
                {
                    Title = s.Title,
                    Fields = s.Fields.ConvertAll(f => new FormField
                    {
                        Label = f.Label,
                        Type = f.Type,
                        Required = f.Required,
                        Options = f.Options ?? new List<string>()
                    })
                }));

            var result = await _forms.UpdateOneAsync(f => f.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteFormAsync(string id)
        {
            var result = await _forms.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
