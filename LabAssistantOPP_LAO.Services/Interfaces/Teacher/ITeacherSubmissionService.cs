using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Teacher
{
	public interface ITeacherSubmissionService
	{
		Task<List<SubmissionDto>> GetSubmissionsWaitingReviewAsync(string classId);
		Task<SubmissionDto> GetSubmissionDetailAsync(string submissionId);
		Task<bool> GradeSubmissionAsync(string submissionId, bool isPass);
		Task<bool> SubmitFeedbackAsync(string submissionId, string teacherId, string comment);
	}
}
