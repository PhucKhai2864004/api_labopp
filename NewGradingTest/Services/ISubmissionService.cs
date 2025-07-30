using LabAssistantOPP_LAO.Models.Entities;
using NewGradingTest.DTOs;
using NewGradingTest.Models;

namespace NewGradingTest.Services
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
