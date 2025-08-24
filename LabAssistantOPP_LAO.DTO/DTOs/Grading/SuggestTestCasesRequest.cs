using System.ComponentModel.DataAnnotations;

namespace LabAssistantOPP_LAO.DTO.DTOs.Grading
{
    public class SuggestTestCasesRequest
    {
        [Required(ErrorMessage = "Assignment ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Assignment ID must be a positive integer")]
        public int AssignmentId { get; set; }
    }
}
