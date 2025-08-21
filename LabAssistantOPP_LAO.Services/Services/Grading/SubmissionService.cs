using LabAssistantOPP_LAO.DTO.DTOs.Grading;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Business_Logic.Services.Grading
{
	public class SubmissionService : ISubmissionService
	{
		private readonly LabOopChangeV6Context _context;

		public static class TempStorage
		{
			public static Dictionary<int, SubmissionInfo> Submissions { get; } = new();
		}

		public SubmissionService(LabOopChangeV6Context context)
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

		public async Task<int> SaveSubmissionAsync(SubmitCodeDto dto)
		{
			// Path wwwroot/submissions
			var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions");
			if (!Directory.Exists(wwwrootPath))
				Directory.CreateDirectory(wwwrootPath);

			// Kiểm tra xem sinh viên đã có record cho assignment này chưa
			var existing = await _context.StudentLabAssignments
				.FirstOrDefaultAsync(sla =>
					sla.AssignmentId == dto.ProblemId &&
					sla.StudentId == dto.StudentId &&
					sla.SemesterId == dto.SemesterId);

			// Lấy tên file gốc
			var originalFileName = Path.GetFileName(dto.ZipFile.FileName);

			// Thư mục cho submission
			var folder = Path.Combine(wwwrootPath, $"{dto.StudentId}_{dto.ProblemId}_{dto.SemesterId}");
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			// Path zip file sẽ lưu (tên file gốc)
			var zipPath = Path.Combine(folder, originalFileName);

			// Ghi đè file nếu đã tồn tại
			using (var fs = new FileStream(zipPath, FileMode.Create))
			{
				await dto.ZipFile.CopyToAsync(fs);
			}

			// Giải nén để grading
			ZipFile.ExtractToDirectory(zipPath, folder, overwriteFiles: true);

			// Detect main class
			var mainClass = DetectMainClass(folder);

			if (existing != null)
			{
				// Ghi đè Draft cũ
				existing.SubmissionZip = zipPath.Replace(Directory.GetCurrentDirectory() + "\\wwwroot\\", "");
				existing.SubmittedAt = DateTime.UtcNow;
				// Status giữ nguyên Draft
				existing.LocResult = 0;
				existing.ManuallyEdited = false;

				_context.StudentLabAssignments.Update(existing);
				await _context.SaveChangesAsync();

				// Cache info để grading
				TempStorage.Submissions[existing.Id] = new SubmissionInfo
				{
					SubmissionId = existing.Id,
					ProblemId = dto.ProblemId,
					MainClass = mainClass,
					WorkDir = folder
				};

				return existing.Id;
			}
			else
			{
				// Tạo record mới
				var sla = new StudentLabAssignment
				{
					AssignmentId = dto.ProblemId,
					StudentId = dto.StudentId,
					SemesterId = dto.SemesterId,
					SubmissionZip = zipPath.Replace(Directory.GetCurrentDirectory() + "\\wwwroot\\", ""),
					Status = "Draft", // mặc định
					SubmittedAt = DateTime.UtcNow,
					LocResult = 0,
					ManuallyEdited = false
				};

				_context.StudentLabAssignments.Add(sla);
				await _context.SaveChangesAsync();

				// Cache info để grading
				TempStorage.Submissions[sla.Id] = new SubmissionInfo
				{
					SubmissionId = sla.Id,
					ProblemId = dto.ProblemId,
					MainClass = mainClass,
					WorkDir = folder
				};

				return sla.Id;
			}
		}



		public async Task<List<TestCase>> GetTestCases(int assignmentId)
		{
			return await _context.TestCases
				.Where(tc => tc.AssignmentId == assignmentId)
				.ToListAsync();
		}

		public async Task SaveResultAsync(int studentLabAssignmentId, List<SubmissionResultDetail> resultDetails)
		{
			// Xóa kết quả cũ
			var existing = await _context.TestCaseResults
				.Where(r => r.StudentLabAssignmentId == studentLabAssignmentId)
				.ToListAsync();
			if (existing.Any())
				_context.TestCaseResults.RemoveRange(existing);

			// Thêm kết quả mới
			var newResults = resultDetails.Select(d => new TestCaseResult
			{
				StudentLabAssignmentId = studentLabAssignmentId,
				TestCaseId = d.TestCaseId, // giờ TestCase.Id là int
				ActualOutput = d.ActualOutput,
				IsPassed = d.Status == "PASS"
			}).ToList();

			_context.TestCaseResults.AddRange(newResults);

			// Cập nhật submission
			var sla = await _context.StudentLabAssignments.FindAsync(studentLabAssignmentId);
			if (resultDetails.Any()) // chỉ update nếu có test case được chấm
			{
				//sla.Status = newResults.All(r => (bool)r.IsPassed) ? "Passed" : "Reject";
				sla.LocResult = resultDetails.Sum(r => r.DurationMs);
				sla.SubmittedAt = DateTime.UtcNow;
			}


			await _context.SaveChangesAsync();
		}

		public async Task<List<SubmissionResultDetail>?> GetResultAsync(int studentLabAssignmentId)
		{
			var results = await _context.TestCaseResults
				.Include(r => r.TestCase)
				.Where(r => r.StudentLabAssignmentId == studentLabAssignmentId)
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

		public Task<SubmissionInfo?> GetSubmissionAsync(int submissionId)
		{
			TempStorage.Submissions.TryGetValue(submissionId, out var info);
			return Task.FromResult(info);
		}

	}
}
