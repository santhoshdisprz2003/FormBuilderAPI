using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    public class FormLayoutDTO
    {
        [Required]
        public FormHeaderCardDTO HeaderCard { get; set; } = new();

        public List<FormFieldDTO> Fields { get; set; } = new();
    }

    public class FormHeaderCardDTO
    {
         public string Id { get; set; } 
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public class FormFieldDTO
    {
        public string QuestionId { get; set; }  
           
        [Required]
        public string Label { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // text, radio, checkbox, date, etc.

        public bool DescriptionEnabled { get; set; } = false;
        public string? Description { get; set; }

        public bool SingleChoice { get; set; } = false;
        public bool MultipleChoice { get; set; } = false;

        public List<FieldOptionDTO> Options { get; set; } = new();

        public string? Format { get; set; }
        public bool Required { get; set; }
        public int Order { get; set; }
    }

    public class FieldOptionDTO
    {
        public string OptionId { get; set; } 
        public string Value { get; set; } = string.Empty;
    }
}
