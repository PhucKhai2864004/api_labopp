using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class ClassDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Subject { get; set; }
		public int Semester { get; set; }
		public string AcademicYear { get; set; }
		public int LocToPass { get; set; }
		public bool IsActive { get; set; }
	}
}
