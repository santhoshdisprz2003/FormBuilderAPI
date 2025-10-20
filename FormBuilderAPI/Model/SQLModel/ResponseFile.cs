using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Model.SQLModel
{
    [Table("ResponseFiles")]
    public class ResponseFile
    {
        [Key]
        public int FileId { get; set; } 

        [Required]
        [ForeignKey(nameof(FormResponse))]
        public int ResponseId { get; set; }

        [Required]
        [MaxLength(100)]
        public string QuestionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        public int FileMaxSize { get; set; }

        [Required]
        public string Base64Content { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public FormResponse FormResponse { get; set; } = null!;
    }
}
