using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewGradingTest.DTOs;
using NewGradingTest.grading_system.backend.Workers;
using NewGradingTest.Models;

namespace NewGradingTest.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
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

			return Ok(problem);
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

			return Ok(problem);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> Get(string id)
		{
			var problem = await _context.LabAssignments
				.Include(p => p.TestCases)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (problem == null) return NotFound();

			return Ok(problem);
		}

		[HttpGet("{id}/testcases")]
		public async Task<IActionResult> GetTestCases(string id)
		{
			var testCases = await _context.TestCases
				.Where(tc => tc.AssignmentId == id)
				.ToListAsync();

			if (!testCases.Any())
				return NotFound("No test cases found for this problem.");

			return Ok(testCases);
		}
	}
}
