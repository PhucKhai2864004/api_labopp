using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class SubmissionDetailDto
	{
		public string Id { get; set; }
		public string StudentId { get; set; }
		public string StudentName { get; set; }

		public string AssignmentId { get; set; }
		public string AssignmentTitle { get; set; }
		public int LocTarget { get; set; }

		public DateTime SubmittedAt { get; set; }
		public int LOC { get; set; }

		public string Status { get; set; }

		public string FilePath { get; set; }

		public List<string> Feedbacks { get; set; }

		public List<TestCaseResultDto> TestCaseResults { get; set; }
	}

	public class TestCaseResultDto
	{
		public string Id { get; set; } = null!;

		public string? TestCaseId { get; set; }

		public string? ActualOutput { get; set; }

		public bool? IsPassed { get; set; }
	}

}
