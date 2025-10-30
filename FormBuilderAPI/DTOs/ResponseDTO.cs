using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    // DTO used for submitting responses (Learners)
    public class ResponseDTO
    {
        public int? ResponseId { get; set; } // Auto-generated in SQL

        [Required]
        public string FormId { get; set; } = string.Empty; // Mongo ObjectId string

        public string SubmittedBy { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public List<ResponseAnswerDTO> Answers { get; set; } = new();

        
    }

    // DTO representing an individual answer
    public class ResponseAnswerDTO
    {
        public int? AnswerId { get; set; } // Auto-generated in SQL

        [Required]
        public string QuestionId { get; set; } = string.Empty; // Corresponds to Mongo FormField.Id

        // For text-based answers
        public string? AnswerText { get; set; }

         public ResponseFileUploadDTO? File { get; set; }
    }

    // DTO used when returning responses (Admin views)
    public class ResponseDetailDTO
    {
        public int ResponseId { get; set; }
        public string FormId { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;

         public string? SubmittedUserName { get; set; } 
        public DateTime SubmittedAt { get; set; }
        public List<ResponseAnswerDTO> Answers { get; set; } = new();
        public List<ResponseFileDTO> Files { get; set; } = new();
    }

    // DTO used to submit file along with response
    public class ResponseFileUploadDTO
    {
        [Required]
        public string QuestionId { get; set; } = string.Empty; // The question associated with the file

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FileType { get; set; } = string.Empty;  // e.g., "image/png", "application/pdf"

        public long FileMaxSize { get; set; }  // In bytes

        [Required]
        public string Base64Content { get; set; } = string.Empty;  // Actual file content in Base64
    }

    // DTO used when returning uploaded files (Admin views)
    public class ResponseFileDTO
    {
        public int ResponseId { get; set; }
        public string QuestionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileMaxSize { get; set; }
        public string Base64Content { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}