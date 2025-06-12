using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class AssignmentDto
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public int LocTarget { get; set; }
		public DateTime? DueDate { get; set; }
		public string Status { get; set; } // "Open", "Closed"

		public int TotalSubmissions { get; set; }
		public int PassedCount { get; set; }
	}

	public class CreateAssignmentRequest
	{
		public string Title { get; set; }
		public string Description { get; set; }
		public int LocTarget { get; set; }
		public DateTime? DueDate { get; set; }
	}

	public class UpdateAssignmentRequest : CreateAssignmentRequest
	{
		public string AssignmentId { get; set; }
	}
}
