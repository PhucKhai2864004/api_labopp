using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class StudentInClassDto
	{
		public int StudentId { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public int TotalAssignments { get; set; }
		public int PassedAssignments { get; set; }
		public int TotalLOC { get; set; }
	}

	public class StudentDetailDto
	{
		public int StudentId { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public List<StudentAssignmentProgress> Progress { get; set; }
	}

	public class StudentAssignmentProgress
	{
		public int AssignmentId { get; set; }
		public string Title { get; set; }
		public string Status { get; set; } // Passed / Reject / Draft
		public int LOC { get; set; }
		public DateTime? SubmittedAt { get; set; }
	}
}
