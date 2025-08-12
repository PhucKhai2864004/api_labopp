using Business_Logic.Interfaces.Workers.Grading;
using LabAssistantOPP_LAO.DTO.DTOs.Grading;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LabAssistantOPP_LAO.WebApi.Controllers.Grading
{
	[ApiController]
	[Route("api/test-case")]
	[Authorize(Roles = "Head Subject,Teacher")]
	public class ProblemController : ControllerBase
	{
		private readonly LabOppContext _context;

		public ProblemController(LabOppContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> Create(CreateProblemDto dto)
		{
			var problemId = Guid.NewGuid().ToString();

			var problem = new LabAssignment
			{
				Id = problemId,
				Title = dto.Title,
				CreatedAt = DateTime.UtcNow,
				CreatedBy = "system",
				UpdatedAt = DateTime.UtcNow,
				UpdatedBy = "system",
				TestCases = dto.TestCases.Select((t, index) => new TestCase
				{
					Id = $"{problemId}_tc{index + 1}",
					Input = t.Input,
					ExpectedOutput = t.ExpectedOutput,
					AssignmentId = problemId
				}).ToList()
			};

			_context.LabAssignments.Add(problem);
			await _context.SaveChangesAsync();

			return Ok(ApiResponse<LabAssignment>.SuccessResponse(problem, "Problem created successfully."));
		}

		[HttpPost("load-from-folder")]
		public async Task<IActionResult> LoadFromFolder([FromQuery] string problemId, [FromQuery] string folderPath)
		{
			var testCases = TestCaseFileLoader.LoadTestCasesFromFolder(folderPath, problemId);

			var problem = new LabAssignment
			{
				Id = problemId,
				Title = $"Problem {problemId} (from folder)",
				CreatedAt = DateTime.UtcNow,
				CreatedBy = "system",
				UpdatedAt = DateTime.UtcNow,
				UpdatedBy = "system",
				TestCases = testCases.Select((tc, i) => new TestCase
				{
					Id = $"{problemId}_tc{i + 1}",
					Input = tc.Input,
					ExpectedOutput = tc.ExpectedOutput,
					AssignmentId = problemId
				}).ToList()
			};

			_context.LabAssignments.Add(problem);
			await _context.SaveChangesAsync();

			return Ok(ApiResponse<LabAssignment>.SuccessResponse(problem, "Problem loaded from folder successfully."));
		}

		[HttpGet("{id}")]  //Tìm theo id của test case
		public async Task<IActionResult> Get(string id)
		{
			var testCase = await _context.TestCases
				.FirstOrDefaultAsync(tc => tc.Id == id);

			if (testCase == null)
				return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));

			return Ok(ApiResponse<TestCase>.SuccessResponse(testCase));
		}


		[HttpGet("{id}/testcases")] // Lấy tất cả test cases của một bài tập
		public async Task<IActionResult> GetTestCases(string id)
		{
			var testCases = await _context.TestCases
				.Where(tc => tc.AssignmentId == id)
				.ToListAsync();

			if (!testCases.Any())
				return NotFound(ApiResponse<object>.ErrorResponse("No test cases found for this problem."));

			return Ok(ApiResponse<List<TestCase>>.SuccessResponse(testCases));
		}
	}
}
