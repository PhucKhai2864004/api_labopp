using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Teacher
{
	public interface ITeacherSubmissionService
	{
		Task<List<SubmissionDto>> GetSubmissionsWaitingReviewAsync(int classId, SubmissionStatus? status = null);
		Task<SubmissionDetailDto> GetSubmissionDetailAsync(int submissionId);
		Task<bool> GradeSubmissionAsync(int submissionId, string status);
		Task<bool> SubmitFeedbackAsync(int submissionId, int teacherId, string comment);
	}
}
