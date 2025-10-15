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
        private readonly IFormBL _formBL;

        public ResponseBL(SQLDbContext sqlContext, IFormBL formBL)
        {
            _sqlContext = sqlContext;
            _formBL = formBL;
        }

        public async Task<Guid> SubmitResponseAsync(ResponseDTO dto)
        {
            // Determine user role
            string userRole = "Learner"; // replace with actual lookup

            // Fetch form layout
            var form = await _formBL.GetFormByIdAsync(dto.FormId, userRole);

            if (form?.Layout?.Fields == null)
                throw new Exception("Form not found or access denied.");

            // Map answers safely
            var answers = dto.Answers?
                .Select(a =>
                {
                    if (a == null) return null;

                    var question = form.Layout.Fields.FirstOrDefault(q => q.QuestionId == a.QuestionId);
                    if (question == null) return null;

                    if (question.Options != null && question.Options.Count > 0)
                    {
                        List<string> selectedOptionIds;

                        if (question.MultipleChoice)
                        {
                            var submittedValues = a.AnswerText?
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(v => v.Trim())
                                .ToList() ?? new List<string>();

                            selectedOptionIds = question.Options
                                .Where(o => !string.IsNullOrEmpty(o.OptionId) && submittedValues.Contains(o.Value))
                                .Select(o => o.OptionId!)
                                .ToList();
                        }
                        else
                        {
                            var submittedValue = a.AnswerText?.Trim() ?? string.Empty;
                            selectedOptionIds = question.Options
                                .Where(o => !string.IsNullOrEmpty(o.OptionId) && o.Value == submittedValue)
                                .Select(o => o.OptionId!)
                                .ToList();
                        }

                        return new FormResponseAnswer
                        {
                            QuestionId = a.QuestionId,
                            AnswerText = System.Text.Json.JsonSerializer.Serialize(selectedOptionIds)
                        };
                    }
                    else
                    {
                        return new FormResponseAnswer
                        {
                            QuestionId = a.QuestionId,
                            AnswerText = a.AnswerText ?? string.Empty
                        };
                    }
                })
                .Where(a => a != null)
                .ToList() ?? new List<FormResponseAnswer>();

            var response = new FormResponse
            {
                FormId = dto.FormId,
                SubmittedBy = dto.SubmittedBy ?? string.Empty,
                SubmittedAt = DateTime.UtcNow,
                Answers = answers
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
                return new List<ResponseDetailDTO>();

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
