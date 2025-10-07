using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Model.SQLModel
{
    [Table("FormResponseAnswers")]
    public class FormResponseAnswer
    {
        [Key]
        public Guid AnswerId { get; set; }

        [Required]
        public Guid FormResponseId { get; set; } // FK to FormResponse

        [Required]
        public string QuestionId { get; set; } = string.Empty; // Mongo FormField.Id

        [Required]
        [MaxLength(2000)]
        public string AnswerText { get; set; } = string.Empty;

        [ForeignKey(nameof(FormResponseId))]
        public FormResponse FormResponse { get; set; } = null!;
    }
}
