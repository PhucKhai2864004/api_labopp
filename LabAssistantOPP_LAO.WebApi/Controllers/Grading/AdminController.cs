using Business_Logic.Interfaces.Workers.Grading;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Grading
{
	[ApiController]
	[Route("api/admin")]
	[Authorize(Roles = "Teacher")]
	public class AdminController : ControllerBase
	{
		private readonly GradingWorkerPool _workerPool;

		public AdminController(GradingWorkerPool workerPool)
		{
			_workerPool = workerPool;
		}

		private string GetTeacherId()
		{
			return User.FindFirst("userId")?.Value
				?? throw new UnauthorizedAccessException("Teacher ID not found in token");
		}

		[HttpPost("start-workers")]
		public async Task<IActionResult> StartDefaultWorkers([FromQuery] string classCode)
		{
			if (string.IsNullOrWhiteSpace(classCode))
				return BadRequest(ApiResponse<string>.ErrorResponse("ClassCode is required."));

			var teacherId = GetTeacherId();
			int defaultCount = 5; // mặc định 5 worker
			await _workerPool.StartAsync(defaultCount, classCode, teacherId);

			return Ok(ApiResponse<string>.SuccessResponse(
				null,
				$"Started {defaultCount} workers for class {classCode} (Teacher {teacherId})"
			));
		}

		[HttpPost("stop-workers")]
		public async Task<IActionResult> StopWorkers()
		{
			var teacherId = GetTeacherId();
			await _workerPool.StopAllForTeacherAsync(teacherId);
			return Ok(ApiResponse<string>.SuccessResponse(
				null,
				"All your workers stopped."
			));
		}

		[HttpGet("status")]
		public async Task<IActionResult> Status()
		{
			var teacherId = GetTeacherId();
			return Ok(ApiResponse<object>.SuccessResponse(new
			{
				running = await _workerPool.IsRunningAsync(teacherId),
				classCode = await _workerPool.GetClassCodeAsync(teacherId)
			}));
		}

		[HttpPost("start-worker/{name}")]
		public async Task<IActionResult> StartNamedWorker(string name)
		{
			var teacherId = GetTeacherId();
			if (await _workerPool.StartWorkerAsync(name, teacherId))
				return Ok(ApiResponse<string>.SuccessResponse(null, $"Worker {name} started."));
			return BadRequest(ApiResponse<string>.ErrorResponse($"Worker {name} already running."));
		}

		[HttpPost("stop-worker/{name}")]
		public async Task<IActionResult> StopNamedWorker(string name)
		{
			var teacherId = GetTeacherId();
			try
			{
				if (await _workerPool.StopWorkerAsync(name, teacherId))
					return Ok(ApiResponse<string>.SuccessResponse(null, $"Worker {name} stopped."));
				return NotFound(ApiResponse<string>.ErrorResponse($"Worker {name} not found."));
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpGet("workers")]
		public async Task<IActionResult> GetWorkers()
		{
			var teacherId = GetTeacherId();
			return Ok(ApiResponse<object>.SuccessResponse(new
			{
				running = await _workerPool.IsRunningAsync(teacherId),
				classCode = await _workerPool.GetClassCodeAsync(teacherId),
				activeWorkers = await _workerPool.GetActiveWorkerNamesAsync(teacherId)
			}));
		}

	}
}
