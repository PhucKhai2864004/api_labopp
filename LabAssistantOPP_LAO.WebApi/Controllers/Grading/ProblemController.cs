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
		private readonly LabOopChangeV6Context _context;

		public ProblemController(LabOopChangeV6Context context)
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

			var currentUserIdClaim = User.FindFirst("userId")?.Value;
			int? currentUserId = int.TryParse(currentUserIdClaim, out var uid) ? uid : null;

			var now = DateTime.UtcNow;

			if (!string.IsNullOrWhiteSpace(dto.Title))
			{
				assignment.Title = dto.Title;
				assignment.UpdatedAt = now;
				assignment.UpdatedBy = currentUserId;
			}

			var newTestCases = dto.TestCases.Select(t => new TestCase
			{
				AssignmentId = assignment.Id,
				Input = t.Input,
				ExpectedOutput = t.ExpectedOutput,
				Loc = t.Loc,
				CreatedAt = now,
				CreatedBy = null,  // có thể parse userId từ token nếu cần
				UpdatedAt = now,
				UpdatedBy = null
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
			[FromForm] int assignmentId,
			[FromForm] List<IFormFile> files,
			[FromForm] List<string>? descriptions)
		{
			if (files == null || files.Count == 0)
				return BadRequest(ApiResponse<object>.ErrorResponse("No files uploaded."));

			var currentUserId = User.FindFirst("userId")?.Value;
			int? createdBy = int.TryParse(currentUserId, out var uid) ? uid : null;
			var now = DateTime.UtcNow;

			// Tìm assignment
			var assignment = await _context.LabAssignments
				.Include(a => a.TestCases)
				.FirstOrDefaultAsync(a => a.Id == assignmentId);

			if (assignment == null)
			{
				return NotFound(ApiResponse<object>.ErrorResponse("Assignment not found."));
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
			var testCases = TestCaseFileLoader.LoadTestCasesFromFolder(tempFolder, assignmentId, createdBy);

			for (int i = 0; i < testCases.Count; i++)
			{
				if (descriptions != null && i < descriptions.Count)
					testCases[i].Description = descriptions[i]; // 👈 set mô tả nhập tay
				else
					testCases[i].Description = $"Test case {i + 1}";

				assignment.TestCases.Add(testCases[i]);
			}

			await _context.SaveChangesAsync();
			return Ok(ApiResponse<LabAssignment>.SuccessResponse(assignment, "Test cases uploaded successfully."));
		}



		/// <summary>
		/// Lấy thông tin 1 test case theo Id
		/// </summary>
		[HttpGet("testcase/{id:int}")]
		public async Task<IActionResult> GetTestCase(int id)
		{
			var testCase = await _context.TestCases.FirstOrDefaultAsync(tc => tc.Id == id);

			if (testCase == null)
				return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));

			return Ok(ApiResponse<TestCase>.SuccessResponse(testCase));
		}

		/// <summary>
		/// Lấy toàn bộ test case của một problem
		/// </summary>
		[HttpGet("{assignmentId:int}/testcases")]
		public async Task<IActionResult> GetTestCases(int assignmentId)
		{
			var testCases = await _context.TestCases
				.Where(tc => tc.AssignmentId == assignmentId)
				.ToListAsync();

			if (!testCases.Any())
				return NotFound(ApiResponse<object>.ErrorResponse("No test cases found for this assignment."));

			return Ok(ApiResponse<List<TestCase>>.SuccessResponse(testCases));
		}
	}
}
