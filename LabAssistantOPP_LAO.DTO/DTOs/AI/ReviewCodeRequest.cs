using System.ComponentModel.DataAnnotations;
namespace LabAssistantOPP_LAO.DTO.DTOs.AI
{
    public class ReviewCodeRequest
    {
        /// <summary>
        /// ID của assignment (bài tập) - integer từ database
        /// </summary>
        [Required(ErrorMessage = "Assignment ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Assignment ID must be a positive integer")]
        public int AssignmentId { get; set; }

        /// <summary>
        /// ID của submission (bài nộp) - integer từ database
        /// </summary>
        [Required(ErrorMessage = "Submission ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Submission ID must be a positive integer")]
        public int SubmissionId { get; set; }
    }
}
