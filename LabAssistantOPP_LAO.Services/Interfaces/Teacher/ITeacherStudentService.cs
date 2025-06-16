using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Teacher
{
	public interface ITeacherStudentService
	{
		Task<List<StudentInClassDto>> GetStudentsInClassAsync(string classId);
		Task<StudentDetailDto> GetStudentDetailAsync(string classId, string studentId);
	}
}
