using System.ComponentModel.DataAnnotations;

namespace LabAssistantOPP_LAO.DTO.DTOs.Grading
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
        /// Code của student cần review
        /// </summary>
        [Required(ErrorMessage = "Student code is required")]
        public string StudentCode { get; set; } = "";

        /// <summary>
        /// Loại thuật toán (tùy chọn)
        /// </summary>
        public string? AlgorithmType { get; set; }

        /// <summary>
        /// Ngôn ngữ lập trình (tùy chọn)
        /// </summary>
        public string? Language { get; set; } = "Java";
    }
}
