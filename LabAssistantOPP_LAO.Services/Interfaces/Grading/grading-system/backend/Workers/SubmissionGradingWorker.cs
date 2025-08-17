using Business_Logic.Interfaces.Workers.Docker;
using Business_Logic.Services.Grading;
using DotNetCore.CAP;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Business_Logic.Interfaces.Workers.Grading
{
	public class SubmissionGradingWorker : ICapSubscribe
	{
		private readonly ISubmissionService _submissionService;
		private readonly ILogger<SubmissionGradingWorker> _logger;
		private readonly IHubContext<SubmissionHub> _hubContext;
		private readonly DockerRunner _dockerRunner;

		public SubmissionGradingWorker(ISubmissionService submissionService, ILogger<SubmissionGradingWorker> logger, IHubContext<SubmissionHub> hubContext, DockerRunner dockerRunner)
		{
			_submissionService = submissionService;
			_logger = logger;
			_hubContext = hubContext;
			_dockerRunner = dockerRunner;
		}

		// Hàm chính xử lý job
		public async Task HandleAsync(SubmissionJob job)
		{
			_logger.LogInformation($"[Worker] Start grading submission {job.SubmissionId}");

			var testCases = await _submissionService.GetTestCases(job.ProblemId);
			var resultDetails = new List<SubmissionResultDetail>();

			var ioDir = Path.GetFullPath(Path.Combine("temp_io", job.SubmissionId.ToString()));
			var tempWorkerDir = Path.GetFullPath(Path.Combine("worker_temp", job.SubmissionId.ToString()));

			Directory.CreateDirectory(ioDir);
			Directory.CreateDirectory(tempWorkerDir);

			//Copy tất cả file .java từ mọi thư mục con về tempWorkerDir
			var javaFiles = Directory.GetFiles(job.WorkDir, "*.java", SearchOption.AllDirectories);
			foreach (var file in javaFiles)
			{
				var relativePath = Path.GetRelativePath(job.WorkDir, file);
				var destPath = Path.Combine(tempWorkerDir, relativePath);
				Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
				File.Copy(file, destPath, overwrite: true);
			}


			await _hubContext.Clients.Group($"submission_{job.SubmissionId}")
				.SendAsync("GradingStarted", job.SubmissionId);

			foreach (var testCase in testCases)
			{
				var inputPath = Path.Combine(ioDir, "input.txt");
				var expectedPath = Path.Combine(ioDir, "expected.txt");
				var outputPath = Path.Combine(ioDir, "output.txt");

				await File.WriteAllTextAsync(inputPath, testCase.Input);
				await File.WriteAllTextAsync(expectedPath, testCase.ExpectedOutput);

				var executionResult = await _dockerRunner.ExecuteAsync(tempWorkerDir, job.MainClass, inputPath, outputPath);

				string NormalizeNewlines(string text) =>
				text.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

				var expectedOutput = NormalizeNewlines(await File.ReadAllTextAsync(expectedPath));
				var actualOutput = NormalizeNewlines(executionResult.Output);

				string Normalize(string text) =>
				text.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

				resultDetails.Add(new SubmissionResultDetail
				{
					TestCaseId = testCase.Id,
					Status = Normalize(expectedOutput) == Normalize(actualOutput) ? "PASS" : "FAIL",
					ActualOutput = Normalize(actualOutput),
					ExpectedOutput = Normalize(expectedOutput),
					DurationMs = executionResult.DurationMs,
					ErrorLog = executionResult.Stderr
				});


				await _hubContext.Clients.Group($"submission_{job.SubmissionId}")
					.SendAsync("TestCaseGraded", new
					{
						SubmissionId = job.SubmissionId,
						TestCaseId = testCase.Id,
						Status = expectedOutput == actualOutput ? "PASS" : "FAIL",
						DurationMs = executionResult.DurationMs,
						Error = executionResult.Stderr
					});
			}

			await _submissionService.SaveResultAsync(job.SubmissionId, resultDetails);

			_logger.LogInformation($"Submission {job.SubmissionId} result:");
			foreach (var r in resultDetails)
			{
				_logger.LogInformation($"TestCase {r.TestCaseId}: {r.Status} (Expected: {r.ExpectedOutput}, Got: {r.ActualOutput})");
				if (!string.IsNullOrWhiteSpace(r.ErrorLog))
				{
					_logger.LogWarning($"  stderr: {r.ErrorLog}");
				}
			}

			await _hubContext.Clients.Group($"submission_{job.SubmissionId}")
				.SendAsync("GradingFinished", job.SubmissionId);
			_logger.LogInformation($"[Worker] Finished grading {job.SubmissionId}");

			// Cleanup IO folder
			try
			{
				RemoveReadOnly(ioDir);
				Directory.Delete(ioDir, true);
			}
			catch (Exception ex)
			{
				_logger.LogWarning($"[Worker] Failed to delete temp io folder: {ex.Message}");
			}

			// Cleanup worker temp folder (keep original submission folder)
			try
			{
				if (Directory.Exists(tempWorkerDir))
				{
					RemoveReadOnly(tempWorkerDir);
					Directory.Delete(tempWorkerDir, true);
					_logger.LogInformation($"[Worker] Deleted temp worker folder: {tempWorkerDir}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning($"[Worker] Failed to delete temp worker folder: {ex.Message}");
			}
		}

		private void RemoveReadOnly(string folderPath)
		{
			if (!Directory.Exists(folderPath)) return;

			var dir = new DirectoryInfo(folderPath);

			foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
			{
				file.Attributes &= ~FileAttributes.ReadOnly;
			}

			foreach (var subDir in dir.GetDirectories("*", SearchOption.AllDirectories))
			{
				subDir.Attributes &= ~FileAttributes.ReadOnly;
			}

			dir.Attributes &= ~FileAttributes.ReadOnly;
		}

	}
}
