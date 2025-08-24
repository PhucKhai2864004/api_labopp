using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LabAssistantOPP_LAO.DTO.DTOs.AI
{
    public class IngestPdfRequest
    {
        /// <summary>
        /// ID của assignment (bài tập) - REQUIRED (auto-increment integer)
        /// </summary>
        [Required(ErrorMessage = "Assignment ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Assignment ID must be a positive integer")]
        public int AssignmentId { get; set; }

        /// <summary>
        /// File PDF đề bài cần ingest
        /// </summary>
        [Required(ErrorMessage = "PDF file is required")]
        public IFormFile PdfFile { get; set; } = null!;
    }
}
