using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NewGradingTest.DTOs;
using NewGradingTest.Models;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace NewGradingTest.Services
{
	public class SubmissionService : ISubmissionService
	{
		private readonly LabOppContext _context;

		public static class TempStorage
		{
			public static Dictionary<string, SubmissionInfo> Submissions { get; } = new();
		}

		public SubmissionService(LabOppContext context)
		{
			_context = context;
		}

		private string DetectMainClass(string workDir)
		{
			var javaFiles = Directory.GetFiles(workDir, "*.java", SearchOption.AllDirectories);

			foreach (var file in javaFiles)
			{
				var code = File.ReadAllText(file);
				// Kiểm tra có hàm main không
				if (!Regex.IsMatch(code, @"public\s+static\s+void\s+main\s*\("))
					continue;

				// Lấy package
				var packageMatch = Regex.Match(code, @"(?m)^\s*package\s+([\w\.]+)\s*;");
				var packageName = packageMatch.Success ? packageMatch.Groups[1].Value : null;

				// Lấy class name từ file (ưu tiên public class)
				var classMatch = Regex.Match(code, @"(?m)^\s*(public\s+)?class\s+(\w+)");
				if (!classMatch.Success) continue;
				var className = classMatch.Groups[2].Value;

				return packageName != null ? $"{packageName}.{className}" : className;
			}

			// Nếu không tìm thấy file có hàm main
			return "Main"; // fallback
		}

		public async Task<string> SaveSubmissionAsync(SubmitCodeDto dto)
		{
			var submissionId = $"{dto.StudentId}_{dto.ProblemId}";

			var existing = await _context.Submissions.FindAsync(submissionId);
			if (existing != null)
			{
				_context.Submissions.Remove(existing);
				_context.TestCaseResults.RemoveRange(
					_context.TestCaseResults.Where(r => r.SubmissionId == submissionId)
				);
			}

			// 1. Tạo thư mục nộp bài
			var folder = Path.Combine("submissions", submissionId);
			// Xóa thư mục cũ nếu đã tồn tại
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, true);
			}
			Directory.CreateDirectory(folder);

			Directory.CreateDirectory(folder);

			// 2. Lưu file zip vào ổ đĩa
			var zipPath = Path.Combine(folder, "code.zip");
			using (var fs = new FileStream(zipPath, FileMode.Create))
			{
				await dto.ZipFile.CopyToAsync(fs);
			}

			var fileInfo = new FileInfo(zipPath);
			var uploadFile = new UploadFile
			{
				Id = Guid.NewGuid().ToString(),
				OriginName = dto.ZipFile.FileName,
				Name = "code.zip", // hoặc Path.GetFileNameWithoutExtension(zipPath)
				Path = zipPath, // hoặc lưu tương đối nếu không muốn lưu full path
				MimeType = dto.ZipFile.ContentType,
				Size = (int)(new FileInfo(zipPath).Length),
				UploadedBy = dto.StudentId,
				UploadedAt = DateTime.UtcNow
			};
			_context.Files.Add(uploadFile);


			// 3. Giải nén
			ZipFile.ExtractToDirectory(zipPath, folder);
			File.Delete(zipPath);

			// 4. Lưu thông tin tạm phục vụ grading
			var mainClass = DetectMainClass(folder);

			var submissionInfo = new SubmissionInfo
			{
				SubmissionId = submissionId,
				ProblemId = dto.ProblemId,
				MainClass = mainClass,
				WorkDir = folder
			};

			// Bạn có thể dùng cache (ConcurrentDictionary) nếu muốn tạm lưu
			TempStorage.Submissions[submissionId] = submissionInfo;


			// (Optional) Lưu vào bảng Submission trong DB nếu cần
			var submission = new Submission
			{
				Id = submissionId,
				StudentId = dto.StudentId,
				AssignmentId = dto.ProblemId,
				ZipCode = uploadFile.Id, // bỏ qua nếu bạn không lưu File record
				Status = "Draft",
				SubmittedAt = DateTime.UtcNow,
				CreatedBy = dto.StudentId,
				CreatedAt = DateTime.UtcNow,
				UpdatedBy = dto.StudentId,
				UpdatedAt = DateTime.UtcNow,
				LocResult = 0,
				ManuallyEdited = false
			};

			_context.Submissions.Add(submission);
			await _context.SaveChangesAsync();

			return submissionId;
		}


		public async Task<List<TestCase>> GetTestCases(string assignmentId)
		{
			return await _context.TestCases
				.Where(tc => tc.AssignmentId == assignmentId)
				.ToListAsync();
		}

		public async Task SaveResultAsync(string submissionId, List<SubmissionResultDetail> resultDetails)
		{
			// Xóa kết quả cũ (nếu có)
			var existing = await _context.TestCaseResults
				.Where(r => r.SubmissionId == submissionId)
				.ToListAsync();

			if (existing.Any())
			{
				_context.TestCaseResults.RemoveRange(existing);
			}

			// Thêm kết quả mới
			var newResults = resultDetails.Select(d => new TestCaseResult
			{
				Id = Guid.NewGuid().ToString(),
				SubmissionId = submissionId,
				TestCaseId = d.TestCaseId,
				ActualOutput = d.ActualOutput,
				IsPassed = d.Status == "PASS"
			}).ToList();

			_context.TestCaseResults.AddRange(newResults);

			// Cập nhật trạng thái submission
			var submission = await _context.Submissions.FindAsync(submissionId);
			if (submission != null)
			{
				submission.Status = newResults.All(r => (bool)r.IsPassed) ? "Passed" : "Reject";
				submission.LocResult = resultDetails.Sum(r => r.DurationMs); // hoặc tính lại LOC nếu có
				submission.UpdatedAt = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();
		}

		public async Task<List<SubmissionResultDetail>?> GetResultAsync(string submissionId)
		{
			var results = await _context.TestCaseResults
				.Include(r => r.TestCase)
				.Where(r => r.SubmissionId == submissionId)
				.ToListAsync();

			if (!results.Any())
				return null;

			return results.Select(r => new SubmissionResultDetail
			{
				TestCaseId = r.TestCaseId,
				Status = (bool)r.IsPassed ? "PASS" : "FAIL",
				ActualOutput = r.ActualOutput,
				ExpectedOutput = r.TestCase?.ExpectedOutput ?? "",
				DurationMs = 0, // không lưu trong DB, chỉ tính runtime nếu muốn
				ErrorLog = ""
			}).ToList();
		}

		public Task<SubmissionInfo?> GetSubmissionAsync(string submissionId)
		{
			TempStorage.Submissions.TryGetValue(submissionId, out var info);
			return Task.FromResult(info);
		}

	}
}
