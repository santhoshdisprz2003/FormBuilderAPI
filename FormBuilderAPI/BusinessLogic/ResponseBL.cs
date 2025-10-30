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

        public async Task<int> SubmitResponseAsync(ResponseDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            string userRole = "Learner";

            // 1️⃣ Fetch the form layout from Mongo
            var form = await _formBL.GetFormByIdAsync(dto.FormId, userRole);
            if (form?.Layout?.Fields == null)
                throw new Exception("Form not found or access denied.");

            // 2️⃣ Filter & map only non-file answers
            var answers = dto.Answers?
                .Where(a =>
                {
                    var question = form.Layout.Fields.FirstOrDefault(q => q.QuestionId == a.QuestionId);
                    // ✅ Skip file-type questions
                    return question != null && !string.Equals(question.Type, "file-upload", StringComparison.OrdinalIgnoreCase);
                })
                .Select(a =>
                {
                    var question = form.Layout.Fields.First(q => q.QuestionId == a.QuestionId);

                    // Handle multiple-choice or single-choice
                    if (question.Type == "drop-down")
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
                .ToList() ?? new List<FormResponseAnswer>();

            // 3️⃣ Create and save FormResponse
            var response = new FormResponse
            {
                FormId = dto.FormId,
                SubmittedBy = dto.SubmittedBy ?? string.Empty,
                SubmittedAt = DateTime.UtcNow,
                Answers = answers
            };

            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();

            // 4️⃣ Handle file uploads ONLY for file-type questions
            long maxFileSize = 5 * 1024 * 1024; // 5 MB
            string[] allowedTypes = { "jpg", "jpeg", "png", "pdf", "docx" };

            foreach (var answer in dto.Answers)
            {
                var file = answer.File;
                if (file == null) continue;

                var question = form.Layout.Fields.FirstOrDefault(q => q.QuestionId == file.QuestionId);
                if (question == null || !string.Equals(question.Type, "file-upload", StringComparison.OrdinalIgnoreCase))
                    continue; // ✅ Process only file-type questions

                // Validate file size
                if (file.FileMaxSize > maxFileSize)
                    throw new Exception($"File '{file.FileName}' exceeds the 5 MB limit.");

                // Validate extension
                var extension = file.FileName.Split('.').LastOrDefault()?.ToLower();
                if (extension == null || !allowedTypes.Contains(extension))
                    throw new Exception($"File '{file.FileName}' has invalid type. Allowed types: {string.Join(',', allowedTypes)}");

                // Save file record
                var responseFile = new ResponseFile
                {
                    ResponseId = response.ResponseId,
                    QuestionId = file.QuestionId,
                    FileName = file.FileName,
                    FileType = file.FileType,
                    FileMaxSize = (int)file.FileMaxSize,
                    Base64Content = file.Base64Content,
                    UploadedAt = DateTime.UtcNow
                };

                _sqlContext.ResponseFiles.Add(responseFile);
            }

            await _sqlContext.SaveChangesAsync();

            // 5️⃣ Return the new response id
            return response.ResponseId;
        }


        public async Task<List<ResponseDetailDTO>> GetResponsesForFormAsync(string formId)
        {
            // 1️⃣ Fetch all responses for the given form including answers
            var responses = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId)
                .ToListAsync();

            // 2️⃣ Collect distinct SubmittedBy user IDs (as string since SubmittedBy might be string)
            var userIds = responses
                .Select(r => r.SubmittedBy)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            // 3️⃣ Fetch corresponding usernames from Users table
            var users = await _sqlContext.Users
                .Where(u => userIds.Contains(u.UserId.ToString()))
                .ToDictionaryAsync(u => u.UserId.ToString(), u => u.Username);

            // 4️⃣ Map the results into DTO
            return responses.Select(r => new ResponseDetailDTO
            {
                ResponseId = r.ResponseId,
                FormId = r.FormId,
                SubmittedBy = r.SubmittedBy,
                SubmittedUserName = users.ContainsKey(r.SubmittedBy) ? users[r.SubmittedBy] : "Unknown",
                SubmittedAt = r.SubmittedAt,
                Answers = r.Answers.Select(a => new ResponseAnswerDTO
                {
                    AnswerId = a.AnswerId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText
                }).ToList(),
                Files = _sqlContext.ResponseFiles
                    .Where(f => f.ResponseId == r.ResponseId)
                    .Select(f => new ResponseFileDTO
                    {
                        ResponseId = f.ResponseId,
                        QuestionId = f.QuestionId,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        FileMaxSize = f.FileMaxSize,
                        Base64Content = f.Base64Content,
                        UploadedAt = f.UploadedAt
                    }).ToList()
            }).ToList();
        }

        public async Task<List<ResponseDetailDTO>> GetAllResponsesByUserAsync(string userId)
        {
            // 1️⃣ Fetch all responses submitted by this user across all forms
            var responses = await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.SubmittedBy == userId)
                .ToListAsync();

            if (responses == null || responses.Count == 0)
                return new List<ResponseDetailDTO>();

            // 2️⃣ Get all related form IDs and fetch form titles/descriptions if needed
            var formIds = responses
                .Select(r => r.FormId)
                .Distinct()
                .ToList();

            // 3️⃣ Fetch form details (optional, if you want to show form name in response list)
            var forms = new Dictionary<string, string>();
            foreach (var formId in formIds)
            {
                try
                {
                    var form = await _formBL.GetFormByIdAsync(formId, "Admin");
                    if (form?.Config?.Title != null)
                        forms[formId] = form.Config.Title;
                }
                catch
                {
                    forms[formId] = "Unknown Form";
                }
            }

            // 4️⃣ Map to DTOs
            return responses.Select(r => new ResponseDetailDTO
            {
                ResponseId = r.ResponseId,
                FormId = r.FormId,
                FormTitle = forms.ContainsKey(r.FormId) ? forms[r.FormId] : "Unknown Form",
                SubmittedBy = r.SubmittedBy,
                SubmittedAt = r.SubmittedAt,
                Answers = r.Answers.Select(a => new ResponseAnswerDTO
                {
                    AnswerId = a.AnswerId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText
                }).ToList(),
                Files = _sqlContext.ResponseFiles
                    .Where(f => f.ResponseId == r.ResponseId)
                    .Select(f => new ResponseFileDTO
                    {
                        ResponseId = f.ResponseId,
                        QuestionId = f.QuestionId,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        FileMaxSize = f.FileMaxSize,
                        Base64Content = f.Base64Content,
                        UploadedAt = f.UploadedAt
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

        public async Task<ResponseFileDTO?> GetFileByResponseIdAndFileIdAsync(int responseId, int fileId)
        {
            var file = await _sqlContext.ResponseFiles
                .Where(f => f.ResponseId == responseId && f.FileId == fileId)
                .FirstOrDefaultAsync();

            if (file == null)
                return null;

            return new ResponseFileDTO
            {
                ResponseId = file.ResponseId,
                QuestionId = file.QuestionId,
                FileName = file.FileName,
                FileType = file.FileType,
                FileMaxSize = file.FileMaxSize,
                Base64Content = file.Base64Content,
                UploadedAt = file.UploadedAt
            };
        }

    }
}
