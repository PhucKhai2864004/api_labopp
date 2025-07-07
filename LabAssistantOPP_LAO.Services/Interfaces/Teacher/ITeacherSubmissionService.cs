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
		Task<List<SubmissionDto>> GetSubmissionsWaitingReviewAsync(string classId, SubmissionStatus? status = null);
		Task<SubmissionDetailDto> GetSubmissionDetailAsync(string submissionId);
		Task<bool> GradeSubmissionAsync(string submissionId, bool isPass);
		Task<bool> SubmitFeedbackAsync(string submissionId, string teacherId, string comment);
	}
}
