using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class ResponseBL : IResponseBL
    {
        private readonly SQLDbContext _sqlContext;

        public ResponseBL(SQLDbContext sqlContext)
        {
            _sqlContext = sqlContext;
        }

        // Learner submits a form response
        public async Task<Guid> SubmitResponseAsync(ResponseDTO dto)
        {
            var response = new FormResponse
            {
                FormId = dto.FormId,
                SubmittedBy = dto.SubmittedBy,
                SubmittedAt = DateTime.UtcNow,
                Answers = dto.Answers.Select(a => new FormResponseAnswer
                {
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText
                }).ToList()
            };

            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            return response.ResponseId;
        }

        // Admin: get all responses for a specific form
        public async Task<List<ResponseDetailDTO>> GetResponsesForFormAsync(string formId)
        {
            var responses = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId)
                .ToListAsync();

            return responses.Select(MapToDetailDTO).ToList();
        }

        // Admin: get a specific response by Guid
        public async Task<ResponseDetailDTO?> GetResponseByIdAsync(Guid id)
        {
            var response = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.ResponseId == id);

            return response == null ? null : MapToDetailDTO(response);
        }

        // Admin: get all responses (any form)
        public async Task<List<ResponseDetailDTO>> GetAllResponsesAsync()
        {
            var responses = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .ToListAsync();

            return responses.Select(MapToDetailDTO).ToList();
        }

        // Admin: delete a response
        public async Task<bool> DeleteResponseAsync(Guid id)
        {
            var response = await _sqlContext.FormResponses.FindAsync(id);
            if (response == null)
                return false;

            _sqlContext.FormResponses.Remove(response);
            await _sqlContext.SaveChangesAsync();
            return true;
        }

        // 🔧 Private mapper helper
        private static ResponseDetailDTO MapToDetailDTO(FormResponse r) =>
            new ResponseDetailDTO
            {
                ResponseId = r.ResponseId,
                FormId = r.FormId,
                SubmittedBy = r.SubmittedBy,
                SubmittedAt = r.SubmittedAt,
                Answers = r.Answers.Select(a => new ResponseAnswerDTO
                {
                    AnswerId = a.AnswerId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText
                }).ToList()
            };
    }
}
