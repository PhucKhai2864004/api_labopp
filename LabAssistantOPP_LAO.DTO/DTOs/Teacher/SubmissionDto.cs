using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class SubmissionDto
	{
		public string Id { get; set; }
		public string StudentName { get; set; }
		public string AssignmentCode { get; set; }
		public DateTime SubmittedAt { get; set; }
		public int LOC { get; set; }
		public string Status { get; set; }
		public string FilePath { get; set; }
		public string Comment { get; set; }
	}

	public class GradeSubmissionRequest
	{
		public string SubmissionId { get; set; }
		public bool IsPass { get; set; }
	}

	public class FeedbackRequest
	{
		public string SubmissionId { get; set; }
		public string Comment { get; set; }
	}
}
