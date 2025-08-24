using Business_Logic.Services.Grading;
using Business_Logic.Interfaces.Grading;
using DotNetCore.CAP;
using LabAssistantOPP_LAO.DTO.DTOs.Grading;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Grading
{
	[ApiController]
	[Route("api/submit")]
	[Authorize(Roles = "Student")]
	public class SubmissionController : ControllerBase
	{
		private readonly ISubmissionService _submissionService;
		private readonly IAIService _aiService;
		private readonly ICapPublisher _capBus;
		private readonly LabOopChangeV6Context _context;
		private readonly ILogger<SubmissionController> _logger;

		public SubmissionController(
			ISubmissionService submissionService, 
			IAIService aiService,
			ICapPublisher capBus, 
			LabOopChangeV6Context context,
			ILogger<SubmissionController> logger)
		{
			_submissionService = submissionService;
			_aiService = aiService;
			_capBus = capBus;
			_context = context;
			_logger = logger;
		}

		[HttpPost]
		public async Task<IActionResult> Submit([FromForm] SubmitCodeDto dto)
		{
			var submissionId = await _submissionService.SaveSubmissionAsync(dto);

			var submission = await _submissionService.GetSubmissionAsync(submissionId);
			if (submission == null)
				return BadRequest(ApiResponse<object>.ErrorResponse("Invalid submission"));

			// Nếu là Draft thì mới grading và AI review
			if (submission.Status == "Draft")
			{
				var teacherId = await _context.LabAssignments
					.Where(a => a.Id == submission.ProblemId)
					.Select(a => a.TeacherId)
					.FirstOrDefaultAsync();

				if (teacherId == 0)
					return BadRequest(ApiResponse<object>.ErrorResponse("TeacherId not found"));

				// Trigger traditional grading
				var job = new SubmissionJob
				{
					SubmissionId = submission.SubmissionId,
					ProblemId = submission.ProblemId,
					WorkDir = submission.WorkDir,
					MainClass = submission.MainClass,
					TeacherId = teacherId
				};

				await _capBus.PublishAsync("submission.created", job);

				// Trigger AI Review (background task)
				_ = Task.Run(async () =>
				{
					try
					{
						await PerformAIReviewAsync(submission.ProblemId, submission.WorkDir);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error performing AI review for submission {SubmissionId}", submissionId);
					}
				});

				return Ok(ApiResponse<object>.SuccessResponse(
					new { submissionId },
					"Draft received. Grading and AI review in progress."
				));
			}

			// Nếu là Submit thì không grading nữa
			return Ok(ApiResponse<object>.SuccessResponse(
				new { submissionId },
				"Submission finalized. No grading executed."
			));
		}


		[HttpGet("{submissionId:int}/result")]
		public async Task<IActionResult> GetResult(int submissionId)
		{
			var result = await _submissionService.GetResultAsync(submissionId);
			if (result == null)
				return NotFound(ApiResponse<object>.ErrorResponse("Result not found"));

					return Ok(ApiResponse<List<SubmissionResultDetail>>.SuccessResponse(result));
	}

	/// <summary>
	/// Perform AI Review for submitted code
	/// </summary>
	private async Task PerformAIReviewAsync(int problemId, string workDir)
	{
		try
		{
			_logger.LogInformation("Starting AI review for problem {ProblemId}, workDir: {WorkDir}", problemId, workDir);

			// Extract Java code from work directory
			var studentCode = ExtractJavaCodeFromDirectory(workDir);
			
			if (string.IsNullOrWhiteSpace(studentCode))
			{
				_logger.LogWarning("No Java code found in directory {WorkDir}", workDir);
				return;
			}

			// Call AI Service for review
			var reviewResult = await _aiService.ReviewCodeAsync(problemId.ToString(), studentCode);
			
			if (reviewResult.ReviewAllowed)
			{
				_logger.LogInformation("AI review completed for problem {ProblemId}. HasErrors: {HasErrors}, ErrorCount: {ErrorCount}", 
					problemId, reviewResult.HasErrors, reviewResult.ErrorCount);
			}
			else
			{
				_logger.LogWarning("AI review not allowed for problem {ProblemId}. Error: {Error}", 
					problemId, reviewResult.Error);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in PerformAIReviewAsync for problem {ProblemId}", problemId);
		}
	}

	/// <summary>
	/// Extract Java code from extracted directory
	/// </summary>
	private string ExtractJavaCodeFromDirectory(string workDir)
	{
		try
		{
			if (!Directory.Exists(workDir))
			{
				_logger.LogWarning("Work directory does not exist: {WorkDir}", workDir);
				return "";
			}

			// Find all .java files
			var javaFiles = Directory.GetFiles(workDir, "*.java", SearchOption.AllDirectories);
			
			if (!javaFiles.Any())
			{
				_logger.LogWarning("No Java files found in directory: {WorkDir}", workDir);
				return "";
			}

			var codeBuilder = new StringBuilder();
			
			foreach (var javaFile in javaFiles)
			{
				var fileName = Path.GetFileName(javaFile);
				var content = System.IO.File.ReadAllText(javaFile);
				
				codeBuilder.AppendLine($"// File: {fileName}");
				codeBuilder.AppendLine(content);
				codeBuilder.AppendLine();
			}

			return codeBuilder.ToString();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error extracting Java code from directory {WorkDir}", workDir);
			return "";
		}
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
