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
		[HttpPost("load-from-files")]
		public async Task<IActionResult> LoadFromFiles(
	[FromForm] string problemId,
	[FromForm] List<IFormFile> files)
		{
			if (files == null || files.Count == 0)
				return BadRequest(ApiResponse<object>.ErrorResponse("No files uploaded."));

			var currentUser = User.Identity?.Name ?? "system";
			var now = DateTime.UtcNow;

			// Tìm assignment
			var assignment = await _context.LabAssignments
				.Include(a => a.TestCases)
				.FirstOrDefaultAsync(a => a.Id == problemId);

			if (assignment == null)
			{
				// Nếu không có thì tạo mới
				assignment = new LabAssignment
				{
					Id = problemId,
					Title = $"Problem {problemId} (uploaded)",
					CreatedAt = now,
					CreatedBy = currentUser,
					UpdatedAt = now,
					UpdatedBy = currentUser,
					TestCases = new List<TestCase>()
				};
				_context.LabAssignments.Add(assignment);
			}
			else
			{
				// Nếu có thì cập nhật
				assignment.UpdatedAt = now;
				assignment.UpdatedBy = currentUser;
			}

			// Lưu file tạm
			var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempFolder);

			foreach (var file in files)
			{
				var filePath = Path.Combine(tempFolder, file.FileName);
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}
			}

			// Load test case
			var testCases = TestCaseFileLoader.LoadTestCasesFromFolder(tempFolder, problemId);

			// Xác định index bắt đầu
			var startingIndex = assignment.TestCases.Count + 1;

			foreach (var (tc, index) in testCases.Select((tc, i) => (tc, i)))
			{
				tc.Id = $"{problemId}_tc{startingIndex + index}";
				tc.AssignmentId = problemId;
				tc.CreatedAt = now;
				tc.CreatedBy = currentUser;
				tc.UpdatedAt = now;
				tc.UpdatedBy = currentUser;
				assignment.TestCases.Add(tc);
			}

			await _context.SaveChangesAsync();
			return Ok(ApiResponse<LabAssignment>.SuccessResponse(assignment, "Test cases uploaded successfully."));
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
