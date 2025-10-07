using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    // DTO used for submitting responses (Learners)
    public class ResponseDTO
    {
        public Guid? ResponseId { get; set; } // Auto-generated in SQL

        [Required]
        public string FormId { get; set; } = string.Empty; // Mongo ObjectId string

        [Required]
        public string SubmittedBy { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public List<ResponseAnswerDTO> Answers { get; set; } = new();
    }

    // DTO representing an individual answer
    public class ResponseAnswerDTO
    {
        public Guid? AnswerId { get; set; } // Auto-generated in SQL

        [Required]
        public string QuestionId { get; set; } = string.Empty; // Corresponds to Mongo FormField.Id

        [Required]
        public string AnswerText { get; set; } = string.Empty;
    }

    // DTO used when returning responses (Admin views)
    public class ResponseDetailDTO
    {
        public Guid ResponseId { get; set; }
        public string FormId { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public List<ResponseAnswerDTO> Answers { get; set; } = new();
    }
}
