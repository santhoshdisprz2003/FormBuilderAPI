using FormBuilderAPI.DTOs;
using FormBuilderAPI.Model.SQLModel;
using FormBuilderAPI.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class ResponseBL : IResponseBL
    {
        private readonly IResponseRepository _responseRepository;
        private readonly IFormBL _formBL;

        // File validation constants
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] ALLOWED_FILE_TYPES = { "jpg", "jpeg", "png", "pdf", "docx" };

        public ResponseBL(IResponseRepository responseRepository, IFormBL formBL)
        {
            _responseRepository = responseRepository;
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

            // 2️⃣ Process non-file answers
            var answers = ProcessNonFileAnswers(dto, form);

            // 3️⃣ Create and save FormResponse
            var response = new FormResponse
            {
                FormId = dto.FormId,
                SubmittedBy = dto.SubmittedBy ?? string.Empty,
                SubmittedAt = DateTime.UtcNow,
                Answers = answers
            };

            var responseId = await _responseRepository.InsertResponseAsync(response);

            // 4️⃣ Handle file uploads
            await ProcessFileUploads(dto, form, responseId);

            return responseId;
        }

        public async Task<object> GetResponsesForFormAsync(
            string formId,
            string? search = null,
            int pageNumber = 1,
            int pageSize = 6)
        {
            // 1️⃣ Fetch all responses for the given form
            var responses = await _responseRepository.GetResponsesByFormIdAsync(formId);

            if (responses == null || responses.Count == 0)
                return CreateEmptyPagedResult(pageNumber, pageSize);

            // 2️⃣ Get user information
            var userIds = responses
                .Select(r => r.SubmittedBy)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var users = await _responseRepository.GetUserNamesByIdsAsync(userIds);

            // 3️⃣ Map responses with files
            var result = await MapResponsesWithFiles(responses, users);

            // 4️⃣ Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                result = ApplySearchFilter(result, search);
            }

            // 5️⃣ Apply pagination
            return ApplyPagination(result, pageNumber, pageSize);
        }

        public async Task<object> GetAllResponsesByUserAsync(
            string userId,
            string? search = null,
            int pageNumber = 1,
            int pageSize = 6)
        {
            var responses = await _responseRepository.GetResponsesByUserIdAsync(userId);

            if (responses == null || responses.Count == 0)
                return CreateEmptyPagedResult(pageNumber, pageSize);

            // Get form information
            var formIds = responses.Select(r => r.FormId).Distinct().ToList();
            var forms = await GetFormInformation(formIds);

            // Map to DTOs
            var result = await MapResponsesWithFormInfo(responses, forms);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                result = ApplyUserResponseSearchFilter(result, search);
            }

            // Apply pagination
            return ApplyUserResponsePagination(result, pageNumber, pageSize);
        }

        public async Task<List<ResponseDetailDTO>> GetResponsesForUserAsync(string formId, string userId)
        {
            var responses = await _responseRepository.GetResponsesByFormIdAndUserIdAsync(formId, userId);

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
            var file = await _responseRepository.GetFileByResponseIdAndFileIdAsync(responseId, fileId);

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

        #region Private Helper Methods

        private List<FormResponseAnswer> ProcessNonFileAnswers(ResponseDTO dto, Model.MongoModel.Form form)
        {
            return dto.Answers?
                .Where(a =>
                {
                    var question = form.Layout.Fields.FirstOrDefault(q => q.QuestionId == a.QuestionId);
                    return question != null && !string.Equals(question.Type, "file-upload", StringComparison.OrdinalIgnoreCase);
                })
                .Select(a =>
                {
                    var question = form.Layout.Fields.First(q => q.QuestionId == a.QuestionId);

                    if (question.Type == "drop-down")
                    {
                        return ProcessDropdownAnswer(a, question);
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
        }

        private FormResponseAnswer ProcessDropdownAnswer(ResponseAnswerDTO answer, Model.MongoModel.FormField question)
        {
            List<string> selectedOptionIds;

            if (question.MultipleChoice)
            {
                var submittedValues = answer.AnswerText?
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
                var submittedValue = answer.AnswerText?.Trim() ?? string.Empty;
                selectedOptionIds = question.Options
                    .Where(o => !string.IsNullOrEmpty(o.OptionId) && o.Value == submittedValue)
                    .Select(o => o.OptionId!)
                    .ToList();
            }

            return new FormResponseAnswer
            {
                QuestionId = answer.QuestionId,
                AnswerText = System.Text.Json.JsonSerializer.Serialize(selectedOptionIds)
            };
        }

        private async Task ProcessFileUploads(ResponseDTO dto, Model.MongoModel.Form form, int responseId)
        {
            foreach (var answer in dto.Answers ?? new List<ResponseAnswerDTO>())
            {
                var file = answer.File;
                if (file == null) continue;

                var question = form.Layout.Fields.FirstOrDefault(q => q.QuestionId == file.QuestionId);
                if (question == null || !string.Equals(question.Type, "file-upload", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Validate file
                ValidateFile(file);

                // Save file record
                var responseFile = new ResponseFile
                {
                    ResponseId = responseId,
                    QuestionId = file.QuestionId,
                    FileName = file.FileName,
                    FileType = file.FileType,
                    FileMaxSize = (int)file.FileMaxSize,
                    Base64Content = file.Base64Content,
                    UploadedAt = DateTime.UtcNow
                };

                await _responseRepository.InsertResponseFileAsync(responseFile);
            }
        }

        private void ValidateFile(ResponseFileUploadDTO file)
        {
            // Validate file size
            if (file.FileMaxSize > MAX_FILE_SIZE)
                throw new Exception($"File '{file.FileName}' exceeds the 5 MB limit.");

            // Validate extension
            var extension = file.FileName.Split('.').LastOrDefault()?.ToLower();
            if (extension == null || !ALLOWED_FILE_TYPES.Contains(extension))
                throw new Exception($"File '{file.FileName}' has invalid type. Allowed types: {string.Join(',', ALLOWED_FILE_TYPES)}");
        }

        private async Task<List<object>> MapResponsesWithFiles(
            List<FormResponse> responses,
            Dictionary<string, string> users)
        {
            var result = new List<object>();

            foreach (var r in responses)
            {
                var files = await _responseRepository.GetFilesByResponseIdAsync(r.ResponseId);

                result.Add(new
                {
                    r.ResponseId,
                    r.FormId,
                    r.SubmittedBy,
                    SubmittedUserName = users.ContainsKey(r.SubmittedBy) ? users[r.SubmittedBy] : "Unknown",
                    r.SubmittedAt,
                    Answers = r.Answers.Select(a => new
                    {
                        a.AnswerId,
                        a.QuestionId,
                        a.AnswerText
                    }).ToList(),
                    Files = files.Select(f => new
                    {
                        f.ResponseId,
                        f.QuestionId,
                        f.FileName,
                        f.FileType,
                        f.FileMaxSize,
                        f.Base64Content,
                        f.UploadedAt
                    }).ToList()
                });
            }

            return result;
        }

        private async Task<Dictionary<string, (string Title, string Description)>> GetFormInformation(List<string> formIds)
        {
            var forms = new Dictionary<string, (string Title, string Description)>();

            foreach (var formId in formIds)
            {
                try
                {
                    var form = await _formBL.GetFormByIdAsync(formId, "Admin");
                    forms[formId] = (form?.Config?.Title ?? "Unknown Form", form?.Config?.Description ?? "");
                }
                catch
                {
                    forms[formId] = ("Unknown Form", "");
                }
            }

            return forms;
        }

        private async Task<List<ResponseDetailDTO>> MapResponsesWithFormInfo(
            List<FormResponse> responses,
            Dictionary<string, (string Title, string Description)> forms)
        {
            var result = new List<ResponseDetailDTO>();

            foreach (var r in responses)
            {
                var files = await _responseRepository.GetFilesByResponseIdAsync(r.ResponseId);

                result.Add(new ResponseDetailDTO
                {
                    ResponseId = r.ResponseId,
                    FormId = r.FormId,
                    FormTitle = forms.ContainsKey(r.FormId) ? forms[r.FormId].Title : "Unknown Form",
                    FormDescription = forms.ContainsKey(r.FormId) ? forms[r.FormId].Description : "",
                    SubmittedBy = r.SubmittedBy,
                    SubmittedAt = r.SubmittedAt,
                    Answers = r.Answers.Select(a => new ResponseAnswerDTO
                    {
                        AnswerId = a.AnswerId,
                        QuestionId = a.QuestionId,
                        AnswerText = a.AnswerText
                    }).ToList(),
                    Files = files.Select(f => new ResponseFileDTO
                    {
                        ResponseId = f.ResponseId,
                        QuestionId = f.QuestionId,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        FileMaxSize = f.FileMaxSize,
                        Base64Content = f.Base64Content,
                        UploadedAt = f.UploadedAt
                    }).ToList()
                });
            }

            return result;
        }

        private List<object> ApplySearchFilter(List<object> result, string search)
        {
            var lowerSearch = search.Trim().ToLower();
            return result
                .Where(r =>
                {
                    var response = r as dynamic;
                    var userName = response?.SubmittedUserName as string;
                    var submittedBy = response?.SubmittedBy as string;

                    return (userName != null && userName.ToLower().Contains(lowerSearch)) ||
                           (submittedBy != null && submittedBy.ToLower().Contains(lowerSearch));
                })
                .ToList();
        }

        private List<ResponseDetailDTO> ApplyUserResponseSearchFilter(List<ResponseDetailDTO> result, string search)
        {
            var lowerSearch = search.Trim().ToLower();
            return result.Where(r =>
                (r.FormTitle != null && r.FormTitle.ToLower().Contains(lowerSearch)) ||
                (r.FormDescription != null && r.FormDescription.ToLower().Contains(lowerSearch)) ||
                (r.Answers != null && r.Answers.Any(a =>
                    !string.IsNullOrEmpty(a.AnswerText) && a.AnswerText.ToLower().Contains(lowerSearch)))
            ).ToList();
        }

        private object ApplyPagination(List<object> result, int pageNumber, int pageSize)
        {
            var totalCount = result.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedItems = result
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = pagedItems
            };
        }

        private object ApplyUserResponsePagination(List<ResponseDetailDTO> result, int pageNumber, int pageSize)
        {
            var totalCount = result.Count;
            var pagedItems = result
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private object CreateEmptyPagedResult(int pageNumber, int pageSize)
        {
            return new
            {
                Items = new List<object>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        #endregion
    }
}