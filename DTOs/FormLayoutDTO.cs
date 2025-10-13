using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    public class FormLayoutDTO
    {
        [Required]
        public string FormId { get; set; } = string.Empty; // Link to the form configuration

        [Required]
        public List<FormSectionDTO> Sections { get; set; } = new();
    }
}
