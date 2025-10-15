using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Model.SQLModel
{
    [Table("FormResponseAnswers")]
    public class FormResponseAnswer
    {
        [Key]
        public int AnswerId { get; set; }

        [Required]
        public int ResponseId { get; set; } 

        [Required]
        public string QuestionId { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? AnswerText { get; set; }

        [ForeignKey(nameof(ResponseId))]
        public FormResponse FormResponse { get; set; } = null!;
    }
}
