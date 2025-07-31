using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using NewGradingTest.DTOs;
using NewGradingTest.grading_system.backend.Workers;
using NewGradingTest.Models;
using NewGradingTest.Services;

namespace NewGradingTest.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SubmissionController : ControllerBase
	{
		private readonly ISubmissionService _submissionService;
		//private readonly SubmissionGradingWorker _worker;
		private readonly ICapPublisher _capBus;

		public SubmissionController(ISubmissionService submissionService, ICapPublisher capBus)
		{
			_submissionService = submissionService;
			_capBus = capBus;
		}

		[HttpPost]
		public async Task<IActionResult> Submit([FromForm] SubmitCodeDto dto)
		{
			var submissionId = await _submissionService.SaveSubmissionAsync(dto);

			var submission = await _submissionService.GetSubmissionAsync(submissionId);
			if (submission == null) return BadRequest("Invalid submission");

			var job = new SubmissionJob
			{
				SubmissionId = submission.SubmissionId,
				ProblemId = submission.ProblemId,
				WorkDir = submission.WorkDir,
				MainClass = submission.MainClass
			};

			// ✅ Đưa job vào hàng đợi (Redis queue)
			await _capBus.PublishAsync("submission.created", job);

			return Ok(new { submissionId, message = "Submission received. Grading in progress." });
		}

		[HttpGet("{submissionId}/result")]
		public async Task<IActionResult> GetResult(string submissionId)
		{
			var result = await _submissionService.GetResultAsync(submissionId);
			if (result == null) return NotFound("Result not found");

			return Ok(result);
		}


		//[HttpPost("{submissionId}/grade")]
		//public async Task<IActionResult> Grade(int submissionId)
		//{
		//	var submission = await _submissionService.GetSubmissionAsync(submissionId);
		//	if (submission == null) return NotFound("Submission not found");

		//	var job = new SubmissionJob
		//	{
		//		SubmissionId = submission.SubmissionId,
		//		ProblemId = submission.ProblemId,
		//		WorkDir = submission.WorkDir,
		//		MainClass = submission.MainClass
		//	};

		//	await _worker.HandleAsync(job);

		//	return Ok(new { message = "Grading done." });
		//}


	}
}
