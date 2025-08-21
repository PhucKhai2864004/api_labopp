using LabAssistantOPP_LAO.DTO.DTOs.Grading;
using LabAssistantOPP_LAO.Models.Entities;

namespace Business_Logic.Services.Grading
{
	public interface ISubmissionService
	{
		Task<int> SaveSubmissionAsync(SubmitCodeDto dto);
		Task<List<TestCase>> GetTestCases(int assignmentId);
		Task SaveResultAsync(int studentLabAssignmentId, List<SubmissionResultDetail> resultDetails);
		Task<List<SubmissionResultDetail>?> GetResultAsync(int studentLabAssignmentId);
		Task<SubmissionInfo?> GetSubmissionAsync(int submissionId);
	}
}
