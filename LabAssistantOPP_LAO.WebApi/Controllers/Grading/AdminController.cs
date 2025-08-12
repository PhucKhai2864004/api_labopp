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
		public IActionResult StartDefaultWorkers([FromQuery] string classCode = "DEFAULT", [FromQuery] int count = 3)
		{
			var teacherId = GetTeacherId();
			_workerPool.Start(count, classCode, teacherId);
			return Ok(ApiResponse<string>.SuccessResponse(
				null,
				$"Started {count} workers for class {classCode} (Teacher {teacherId})"
			));
		}

		[HttpPost("stop-workers")]
		public IActionResult StopWorkers()
		{
			var teacherId = GetTeacherId();
			_workerPool.StopAllForTeacher(teacherId);
			return Ok(ApiResponse<string>.SuccessResponse(
				null,
				"All your workers stopped."
			));
		}

		[HttpGet("status")]
		public IActionResult Status()
		{
			var teacherId = GetTeacherId();
			return Ok(ApiResponse<object>.SuccessResponse(new
			{
				running = _workerPool.IsRunning(teacherId)
			}));
		}

		[HttpPost("start-worker/{name}")]
		public IActionResult StartNamedWorker(string name)
		{
			var teacherId = GetTeacherId();
			if (_workerPool.StartWorker(name, teacherId))
				return Ok(ApiResponse<string>.SuccessResponse(null, $"Worker {name} started."));
			return BadRequest(ApiResponse<string>.ErrorResponse($"Worker {name} already running."));
		}

		[HttpPost("stop-worker/{name}")]
		public IActionResult StopNamedWorker(string name)
		{
			var teacherId = GetTeacherId();
			try
			{
				if (_workerPool.StopWorker(name, teacherId))
					return Ok(ApiResponse<string>.SuccessResponse(null, $"Worker {name} stopped."));
				return NotFound(ApiResponse<string>.ErrorResponse($"Worker {name} not found."));
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpGet("workers")]
		public IActionResult GetWorkers()
		{
			var teacherId = GetTeacherId();
			return Ok(ApiResponse<object>.SuccessResponse(new
			{
				running = _workerPool.IsRunning(teacherId),
				activeWorkers = _workerPool.GetActiveWorkerNames(teacherId)
			}));
		}

	}
}
