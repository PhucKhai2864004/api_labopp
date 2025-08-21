using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class SubmissionDto
	{
		public int Id { get; set; }
		public string StudentName { get; set; }
		public int AssignmentCode { get; set; }
		public DateTime SubmittedAt { get; set; }
		public int LOC { get; set; }
		public string Status { get; set; }
		public string FilePath { get; set; }
		public string Comment { get; set; }
	}

    public class GradeSubmissionRequest
    {
        [Required(ErrorMessage = "Submission ID is required")]
        public int SubmissionId { get; set; }

        public bool IsPass { get; set; }
    }

    public class FeedbackRequest
    {
        [Required(ErrorMessage = "Submission ID is required")]
        public int SubmissionId { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string Comment { get; set; }
    }
}
