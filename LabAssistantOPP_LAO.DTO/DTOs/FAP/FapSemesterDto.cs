using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.FAP
{
	public class FapSemesterDto
	{
		public string Code { get; set; }
		public string Name { get; set; }
	}

	public class FapClassDto
	{
		public string Code { get; set; }
		public string Name { get; set; }
		public string SemesterCode { get; set; }
	}

	public class FapStudentDto
	{
		public string StudentCode { get; set; }
		public string Name { get; set; }
		public string SemesterCode { get; set; }
		public string ClassCode { get; set; }
		public string Email { get; set; }  // tạo email để map User
	}

}
