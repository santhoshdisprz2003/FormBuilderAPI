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

        public async Task<List<ResponseDetailDTO>> GetResponsesForFormAsync(string formId)
        {
            var responses = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId)
                .ToListAsync();

            return responses.Select(r => new ResponseDetailDTO
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
            }).ToList();
        }

        public async Task<List<ResponseDetailDTO>> GetResponsesForUserAsync(string formId, string userId)
        {
            var responses = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId && r.SubmittedBy == userId)
                .ToListAsync();

            if (responses == null || responses.Count == 0)
                return new List<ResponseDetailDTO>(); // return empty list instead of null

            return responses.Select(r => new ResponseDetailDTO
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
            }).ToList();
        }

    }
}
