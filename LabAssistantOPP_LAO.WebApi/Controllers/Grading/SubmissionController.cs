using Business_Logic.Services.Grading;
using DotNetCore.CAP;
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
	[Route("api/submit")]
	[Authorize(Roles = "Student")]
	public class SubmissionController : ControllerBase
	{
		private readonly ISubmissionService _submissionService;
		//private readonly SubmissionGradingWorker _worker;
		private readonly ICapPublisher _capBus;
		private readonly LabOppContext _context;

		public SubmissionController(ISubmissionService submissionService, ICapPublisher capBus, LabOppContext context)
		{
			_submissionService = submissionService;
			_capBus = capBus;
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> Submit([FromForm] SubmitCodeDto dto)
		{
			var submissionId = await _submissionService.SaveSubmissionAsync(dto);

			var submission = await _submissionService.GetSubmissionAsync(submissionId);
			if (submission == null)
				return BadRequest(ApiResponse<object>.ErrorResponse("Invalid submission"));

			// 🔴 Nếu là Draft thì không publish job để chấm
			if (dto.Status == "Draft")
			{
				return Ok(ApiResponse<object>.SuccessResponse(
					new { submissionId },
					"Submission saved as Draft. It will not be graded."
				));
			}

			// Nếu là Submit thì mới chấm
			var teacherId = await _context.LabAssignments
				.Where(a => a.Id == submission.ProblemId)
				.Select(a => a.TeacherId)
				.FirstOrDefaultAsync();

			if (string.IsNullOrEmpty(teacherId))
				return BadRequest(ApiResponse<object>.ErrorResponse("TeacherId not found"));

			var job = new SubmissionJob
			{
				SubmissionId = submission.SubmissionId,
				ProblemId = submission.ProblemId,
				WorkDir = submission.WorkDir,
				MainClass = submission.MainClass,
				TeacherId = teacherId
			};

			await _capBus.PublishAsync("submission.created", job);

			return Ok(ApiResponse<object>.SuccessResponse(
				new { submissionId },
				"Submission received. Grading in progress."
			));
		}


		[HttpGet("{submissionId}/result")]
		public async Task<IActionResult> GetResult(string submissionId)
		{
			var result = await _submissionService.GetResultAsync(submissionId);
			if (result == null)
				return NotFound(ApiResponse<object>.ErrorResponse("Result not found"));

			return Ok(ApiResponse<List<SubmissionResultDetail>>.SuccessResponse(result));
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
