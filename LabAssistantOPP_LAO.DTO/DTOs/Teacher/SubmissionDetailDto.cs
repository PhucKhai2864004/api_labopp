using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class SubmissionDetailDto
	{
		public int Id { get; set; }
		public int StudentId { get; set; }
		public string StudentName { get; set; }

		public int AssignmentId { get; set; }
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
		public int Id { get; set; }

		public int? TestCaseId { get; set; }

		public string? ActualOutput { get; set; }

		public bool? IsPassed { get; set; }
	}

}
