using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class TeacherDashboardDto
	{
		public int TotalStudents { get; set; }
		public int TotalAssignments { get; set; }
		public int SubmissionsWaitingReview { get; set; }
		public double PassRate { get; set; } // (%) like 33.0

		public List<RecentAssignmentDto> RecentAssignments { get; set; }
		public List<RecentSubmissionDto> RecentSubmissions { get; set; }
	}

	public class RecentAssignmentDto
	{
		public string Title { get; set; }
		public int Code { get; set; }
		public int TargetLOC { get; set; }
		public int PassedCount { get; set; }
		public int TotalSubmission { get; set; }
	}

	public class RecentSubmissionDto
	{
		public string StudentName { get; set; }
		public int AssignmentCode { get; set; }
		public DateTime SubmittedAt { get; set; }
		public string Status { get; set; } // Pending, Passed, Failed
		public int LOC { get; set; }
	}
}
