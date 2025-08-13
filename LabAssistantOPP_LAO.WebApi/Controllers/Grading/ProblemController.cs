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
			var assignment = await _context.LabAssignments
				.Include(a => a.TestCases)
				.FirstOrDefaultAsync(a => a.Id == dto.AssignmentId);

			if (assignment == null)
			{
				return NotFound(ApiResponse<object>.ErrorResponse("Assignment not found."));
			}

			var currentUser = User.Identity?.Name ?? "system";
			var now = DateTime.UtcNow;

			if (!string.IsNullOrWhiteSpace(dto.Title))
			{
				assignment.Title = dto.Title;
				assignment.UpdatedAt = now;
				assignment.UpdatedBy = currentUser;
			}

			var startingIndex = assignment.TestCases.Count + 1;
			var newTestCases = dto.TestCases.Select((t, index) => new TestCase
			{
				Id = $"{assignment.Id}_tc{startingIndex + index}",
				Input = t.Input,
				ExpectedOutput = t.ExpectedOutput,
				Loc = t.Loc,
				AssignmentId = assignment.Id,
				CreatedAt = now,
				CreatedBy = currentUser,
				UpdatedAt = now,
				UpdatedBy = currentUser
			}).ToList();

			foreach (var tc in newTestCases)
			{
				assignment.TestCases.Add(tc);
			}

			await _context.SaveChangesAsync();

			return Ok(ApiResponse<LabAssignment>.SuccessResponse(assignment, "Test cases added successfully."));
		}




		/// <summary>
		/// Load Problem từ folder test case
		/// </summary>
		[HttpPost("load-from-folder")]
		public async Task<IActionResult> LoadFromFolder([FromQuery] string problemId, [FromQuery] string folderPath)
		{
			var testCases = TestCaseFileLoader.LoadTestCasesFromFolder(folderPath, problemId);

			var labAssignment = new LabAssignment
			{
				Id = problemId,
				Title = $"Problem {problemId} (from folder)",
				CreatedAt = DateTime.UtcNow,
				CreatedBy = User.Identity?.Name ?? "system",
				UpdatedAt = DateTime.UtcNow,
				UpdatedBy = User.Identity?.Name ?? "system",
				TestCases = testCases.Select((tc, i) => new TestCase
				{
					Id = $"{problemId}_tc{i + 1}",
					Input = tc.Input,
					ExpectedOutput = tc.ExpectedOutput,
					AssignmentId = problemId
				}).ToList()
			};

			_context.LabAssignments.Add(labAssignment);
			await _context.SaveChangesAsync();

			return Ok(ApiResponse<LabAssignment>.SuccessResponse(labAssignment, "Problem loaded from folder successfully."));
		}

		/// <summary>
		/// Lấy thông tin 1 test case theo Id
		/// </summary>
		[HttpGet("testcase/{id}")]
		public async Task<IActionResult> GetTestCase(string id)
		{
			var testCase = await _context.TestCases.FirstOrDefaultAsync(tc => tc.Id == id);

			if (testCase == null)
				return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));

			return Ok(ApiResponse<TestCase>.SuccessResponse(testCase));
		}

		/// <summary>
		/// Lấy toàn bộ test case của một problem
		/// </summary>
		[HttpGet("{id}/testcases")]
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
