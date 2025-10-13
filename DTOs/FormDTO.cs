using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    public class FormDTO
    {
        public string? Id { get; set; } // MongoDB ObjectId as string

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;  // Initialize to avoid null warning

        [MaxLength(1000)]
        public string? Description { get; set; }


        public List<FormSectionDTO> Sections { get; set; } = new();
    }

    public class FormSectionDTO
    {
        public string? Id { get; set; } // optional section ID

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;  // Prevent null warning

        public List<FormFieldDTO> Fields { get; set; } = new();
    }

    public class FormFieldDTO
    {
        public string? Id { get; set; } // optional field ID

        [Required]
        [MaxLength(200)]
        public string Label { get; set; } = string.Empty;  // Initialize

        [Required]
        public string Type { get; set; } = string.Empty;  // Initialize

        public bool Required { get; set; } = false;

        // For select, radio, or checkbox types
        public List<string>? Options { get; set; }
    }
}
