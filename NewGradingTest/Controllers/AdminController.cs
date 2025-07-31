using Microsoft.AspNetCore.Mvc;
using NewGradingTest.grading_system.backend.Workers;

namespace NewGradingTest.Controllers
{
	[ApiController]
	[Route("api/admin")]
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
			return Ok($"Started {count} workers for class {classCode}");
		}


		[HttpPost("stop-workers")]
		public IActionResult StopWorkers()
		{
			_workerPool.Stop();
			return Ok("Workers stopped.");
		}

		[HttpGet("status")]
		public IActionResult Status()
		{
			return Ok(new { running = _workerPool.IsRunning });
		}

		[HttpPost("start-worker/{name}")]
		public IActionResult StartNamedWorker(string name) =>
			_workerPool.StartWorker(name)
				? Ok($"Worker {name} started.")
				: BadRequest($"Worker {name} already running.");

		[HttpPost("stop-worker/{name}")]
		public IActionResult StopNamedWorker(string name) =>
			_workerPool.StopWorker(name)
				? Ok($"Worker {name} stopped.")
				: NotFound($"Worker {name} not found.");


		[HttpGet("workers")]
		public IActionResult GetWorkers()
		{
			return Ok(new
			{
				running = _workerPool.IsRunning,
				activeWorkers = _workerPool.GetActiveWorkerNames()
			});
		}

	}
}
