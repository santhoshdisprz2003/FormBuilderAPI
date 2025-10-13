using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FormBuilderAPI.DTOs
{
    // Represent status as string in JSON (e.g., "Draft", "Published")
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FormStatus
    {
        Draft,
        Published
    }

    public class FormDTO
    {
        public string? Id { get; set; } // MongoDB ObjectId as string

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // New: form status (defaults to Draft)
        // NOTE: server should enforce that only Admin can set Publish via Publish endpoint.
        [Required]
        public FormStatus Status { get; set; } = FormStatus.Draft;

        // New: timestamps help UI & auditing — optional from client
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
         public DateTime? PublishedAt { get; set; }

        public List<FormSectionDTO> Sections { get; set; } = new();
    }

    public class FormSectionDTO
    {
        public string? Id { get; set; } // optional section ID

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public List<FormFieldDTO> Fields { get; set; } = new();
    }

    public class FormFieldDTO
    {
        public string? Id { get; set; } // optional field ID

        [Required]
        [MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public bool Required { get; set; } = false;

        // For select, radio, or checkbox types
        public List<string>? Options { get; set; }
    }
}
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
