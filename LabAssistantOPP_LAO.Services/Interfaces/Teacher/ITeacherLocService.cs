using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Teacher
{
	public interface ITeacherLocService
	{
		Task<List<LocRankingDto>> GetLocRankingAsync(int classId);
	}
}
