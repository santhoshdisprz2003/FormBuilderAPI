using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FormBuilderAPI.DTOs
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FormStatus
    {
        Draft,
        Published
    }

    public class FormDTO
    {
        public string? Id { get; set; }

        [Required]
        public FormConfigDTO Config { get; set; } = new();

        [Required]
        public FormLayoutDTO Layout { get; set; } = new();

        [Required]
        public FormStatus Status { get; set; } = FormStatus.Draft;

        [Required]
        public string ?CreatedBy { get; set; } = string.Empty;

        public string? PublishedBy { get; set; }

        public DateTime ?CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
