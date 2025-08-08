using LabAssistantOPP_LAO.DTO.DTOs.Grading;
using LabAssistantOPP_LAO.Models.Entities;

namespace Business_Logic.Services.Grading
{
	public interface ISubmissionService
	{
		Task<string> SaveSubmissionAsync(SubmitCodeDto dto);
		Task<List<TestCase>> GetTestCases(string assignmentId);
		Task SaveResultAsync(string submissionId, List<SubmissionResultDetail> resultDetails);
		Task<List<SubmissionResultDetail>?> GetResultAsync(string submissionId);
		Task<SubmissionInfo?> GetSubmissionAsync(string submissionId);
	}
}
