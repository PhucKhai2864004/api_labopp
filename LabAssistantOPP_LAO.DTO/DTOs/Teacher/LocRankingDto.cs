using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class LocRankingDto
	{
		public int Rank { get; set; }
		public int StudentId { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public int PassedAssignments { get; set; }
		public int TotalLOC { get; set; }
	}
}
