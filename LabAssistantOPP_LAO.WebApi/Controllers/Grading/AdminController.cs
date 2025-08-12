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

		[HttpPost("start-workers")]
		public IActionResult StartDefaultWorkers([FromQuery] string classCode = "DEFAULT", [FromQuery] int count = 3)
		{
			_workerPool.Start(count, classCode);
			return Ok(ApiResponse<string>.SuccessResponse(
				null,
				$"Started {count} workers for class {classCode}"
			));
		}

		[HttpPost("stop-workers")]
		public IActionResult StopWorkers()
		{
			_workerPool.Stop();
			return Ok(ApiResponse<string>.SuccessResponse(
				null,
				"Workers stopped."
			));
		}

		[HttpGet("status")]
		public IActionResult Status()
		{
			return Ok(ApiResponse<object>.SuccessResponse(new
			{
				running = _workerPool.IsRunning
			}));
		}

		[HttpPost("start-worker/{name}")]
		public IActionResult StartNamedWorker(string name)
		{
			if (_workerPool.StartWorker(name))
				return Ok(ApiResponse<string>.SuccessResponse(null, $"Worker {name} started."));
			return BadRequest(ApiResponse<string>.ErrorResponse($"Worker {name} already running."));
		}

		[HttpPost("stop-worker/{name}")]
		public IActionResult StopNamedWorker(string name)
		{
			if (_workerPool.StopWorker(name))
				return Ok(ApiResponse<string>.SuccessResponse(null, $"Worker {name} stopped."));
			return NotFound(ApiResponse<string>.ErrorResponse($"Worker {name} not found."));
		}

		[HttpGet("workers")]
		public IActionResult GetWorkers()
		{
			return Ok(ApiResponse<object>.SuccessResponse(new
			{
				running = _workerPool.IsRunning,
				activeWorkers = _workerPool.GetActiveWorkerNames()
			}));
		}

	}
}
