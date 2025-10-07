using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Model.SQLModel
{
    [Table("FormResponses")]
    public class FormResponse
    {
        [Key]
        public Guid ResponseId { get; set; }

        [Required]
        public string FormId { get; set; } = string.Empty; // Mongo ObjectId as string

        [Required]
        [MaxLength(100)]
        public string SubmittedBy { get; set; } = string.Empty; // UserId (from MongoDB)

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FormResponseAnswer> Answers { get; set; } = new List<FormResponseAnswer>();
    }
}
