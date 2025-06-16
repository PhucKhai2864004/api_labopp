using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Teacher
{
	public interface ITeacherAssignmentService
	{
		Task<List<AssignmentDto>> GetAssignmentsByClassAsync(string classId);
		Task<AssignmentDto> GetAssignmentDetailAsync(string assignmentId);
		Task<string> CreateAssignmentAsync(string classId, string teacherId, CreateAssignmentRequest request);
		Task<bool> UpdateAssignmentAsync(UpdateAssignmentRequest request);
	}
}
