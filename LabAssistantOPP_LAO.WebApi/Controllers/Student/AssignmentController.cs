using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Student
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class AssignmentController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        private readonly LabOopChangeV6Context _context;

        public AssignmentController(LabOopChangeV6Context context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

		[HttpGet("student")]
		public async Task<IActionResult> GetAssignmentsForStudent()
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
			}

			var assignments = await _context.StudentInClasses
				.Where(s => s.StudentId == studentId)
				.Join(
					_context.ClassHasLabAssignments,
					sic => sic.ClassId,
					chla => chla.ClassId,
					(sic, chla) => chla.AssignmentId
				)
				.Distinct()
				.Join(
					_context.LabAssignments,
					assignmentId => assignmentId,
					assignment => assignment.Id,
					(assignmentId, assignment) => assignment
				)
				.Where(a => a.Status == "Active")   // ✅ chỉ lấy bài lab đã duyệt
				.Select(a => new
				{
					a.Id,
					a.Title,
					a.Description,
					a.LocTotal,
					a.CreatedAt
				})
				.ToListAsync();

			return Ok(ApiResponse<object>.SuccessResponse(assignments, "Danh sách bài tập của bạn"));
		}


		[HttpGet("{id:int}")]
		public async Task<IActionResult> GetAssignmentDetail(int id)
		{
			var assignment = await _context.LabAssignments
				.Where(a => a.Id == id && a.Status == "Active")  // ✅ chỉ cho xem lab active
				.Select(a => new AssignmentDetailDto
				{
					Title = a.Title,
					Description = a.Description,
					LocTotal = a.LocTotal
				})
				.FirstOrDefaultAsync();

			if (assignment == null)
			{
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy bài lab hoặc chưa được duyệt."));
			}

			return Ok(ApiResponse<AssignmentDetailDto>.SuccessResponse(assignment, "Lấy chi tiết bài lab thành công"));
		}


		[HttpPost("student-lab-assignment")]
		public async Task<IActionResult> AssignLabToStudent([FromBody] StudentLabAssignmentDto dto)
		{
			if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không thể xác định sinh viên từ token"));
			}

			var existing = await _context.StudentLabAssignments
				.FirstOrDefaultAsync(sla => sla.AssignmentId == dto.AssignmentId
											&& sla.StudentId == studentId
											&& sla.SemesterId == dto.SemesterId);

			if (existing != null)
			{
				return BadRequest(ApiResponse<string>.ErrorResponse("Bạn đã lấy assignment này rồi"));
			}

			var newRecord = new StudentLabAssignment
			{
				AssignmentId = dto.AssignmentId,
				StudentId = studentId,
				SemesterId = dto.SemesterId,
				Status = "Draft"
			};

			_context.StudentLabAssignments.Add(newRecord);
			await _context.SaveChangesAsync();

			return Ok(ApiResponse<int>.SuccessResponse(newRecord.Id, "Lấy assignment thành công"));
		}



		[HttpGet("my-submissions")]
		public async Task<IActionResult> ViewMySubmissions()
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
			}

			var submissions = await _context.StudentLabAssignments
				.Where(s => s.StudentId == studentId)
				.ToListAsync();

			var result = submissions.Select(s => new ViewMySubmissionDto
			{
				SubmissionId = s.Id,
				AssignmentId = s.AssignmentId,
				FileName = Path.GetFileName(s.SubmissionZip), // lấy tên file từ path
				FileUrl = !string.IsNullOrEmpty(s.SubmissionZip)
					? $"/{s.SubmissionZip.Replace("\\", "/")}"
					: null,
				Status = s.Status,
				SubmittedAt = s.SubmittedAt,
				LocResult = s.LocResult,
				ManuallyEdited = s.ManuallyEdited,
				ManualReason = s.ManualReason
			}).ToList();


			return Ok(ApiResponse<List<ViewMySubmissionDto>>.SuccessResponse(result, "Thành công"));
		}

		[HttpGet("my-total-loc")]
		public async Task<IActionResult> GetMyTotalLoc()
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
			}

			var totalLoc = await _context.StudentLabAssignments
				.Where(s => s.StudentId == studentId && s.LocResult != null)
				.SumAsync(s => s.LocResult.Value);

			return Ok(ApiResponse<int>.SuccessResponse(totalLoc, "Tổng LOC của bạn"));
		}

		[HttpGet("my-progress")]
		public async Task<IActionResult> GetMyProgress()
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
			}

			// Tổng LOC
			var totalLoc = await _context.StudentLabAssignments
				.Where(s => s.StudentId == studentId && s.LocResult != null)
				.SumAsync(s => s.LocResult.Value);

			// Tổng assignment của sinh viên
			var totalAssignments = await _context.StudentLabAssignments
				.CountAsync(sla => sla.StudentId == studentId);

			// Tổng bài PASS
			var passedAssignments = await _context.StudentLabAssignments
				.Where(s => s.StudentId == studentId && s.Status == "Passed")
				.Select(s => s.AssignmentId)
				.Distinct()
				.CountAsync();

			// Ranking theo LOC
			var studentLocs = await _context.StudentLabAssignments
				.Where(s => s.LocResult != null)
				.GroupBy(s => s.StudentId)
				.Select(g => new { StudentId = g.Key, TotalLoc = g.Sum(s => s.LocResult.Value) })
				.OrderByDescending(x => x.TotalLoc)
				.ToListAsync();

			var rank = studentLocs.FindIndex(s => s.StudentId == studentId) + 1;
			var totalStudents = studentLocs.Count;

			var result = new Dictionary<string, string>
			{
				["Total LOC"] = $"{totalLoc}/750",
				["Assignment"] = $"{passedAssignments}/{totalAssignments}",
				["Ranking"] = $"{rank}/{totalStudents}"
			};

			return Ok(ApiResponse<Dictionary<string, string>>.SuccessResponse(result, "Tiến độ của bạn"));
		}

		[HttpGet("classmates")]
		public async Task<IActionResult> GetClassmates()
		{
			if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized"));
			}

			var classIds = await _context.StudentInClasses
				.Where(sic => sic.StudentId == studentId)
				.Select(sic => sic.ClassId)
				.ToListAsync();

			if (!classIds.Any())
			{
				return NotFound(ApiResponse<string>.ErrorResponse("You are not enrolled in any class"));
			}

			var classmateIds = await _context.StudentInClasses
				.Where(sic => classIds.Contains(sic.ClassId) && sic.StudentId != studentId)
				.Select(sic => sic.StudentId)
				.Distinct()
				.ToListAsync();

			var classmates = await _context.Users
				.Where(u => classmateIds.Contains(u.Id))
				.Join(_context.Students,
					user => user.Id,
					student => student.Id,
					(user, student) => new
					{
						Id = user.Id,
						Name = user.Name,
						Email = user.Email,
						StudentCode = student.StudentCode,
						Major = student.Major,
						Phone = student.Phone
					})
				.ToListAsync();

			return Ok(ApiResponse<object>.SuccessResponse(classmates, "Classmates retrieved successfully"));
		}

		[HttpGet("my-classes")]
		public async Task<IActionResult> GetMyClasses()
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
			}

			var classes = await (from sic in _context.StudentInClasses
								 join c in _context.Classes on sic.ClassId equals c.Id
								 where sic.StudentId == studentId && c.IsActive
								 select new
								 {
									 c.Id,
									 c.ClassCode,
									 c.SubjectCode,
									 c.Semester,
									 c.AcademicYear,
									 c.LocToPass,
									 c.TeacherId
								 }).ToListAsync();

			return Ok(ApiResponse<object>.SuccessResponse(classes, "Danh sách lớp của bạn"));
		}

		[HttpGet("download-pdf/by-assignment/{assignmentId}")]
		[AllowAnonymous]
		public async Task<IActionResult> DownloadPdfByAssignment(int assignmentId)
		{
			var doc = await _context.AssignmentDocuments
				.FirstOrDefaultAsync(d => d.AssignmentId == assignmentId);

			if (doc == null || string.IsNullOrEmpty(doc.FilePath))
				return NotFound("Không tìm thấy tài liệu PDF cho assignment này.");

			// Ghép đường dẫn thực tế
			var filePath = Path.Combine(
				Directory.GetCurrentDirectory(),
				"wwwroot",
				doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
			);

			if (!System.IO.File.Exists(filePath))
				return NotFound("File không tồn tại trên server");

			var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

			return File(fileBytes, doc.MimeType ?? "application/pdf", doc.FileName);
		}



		[HttpGet("download-submission/{submissionId}")]
		public async Task<IActionResult> DownloadSubmission(int submissionId)
		{
			var sla = await _context.StudentLabAssignments.FindAsync(submissionId);
			if (sla == null || string.IsNullOrEmpty(sla.SubmissionZip))
				return NotFound("Không tìm thấy submission");

			// Ghép đường dẫn thực tế
			var filePath = Path.Combine(
				Directory.GetCurrentDirectory(),
				"wwwroot",
				sla.SubmissionZip.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
			);

			if (!System.IO.File.Exists(filePath))
				return NotFound("File không tồn tại trên server");

			var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
			return File(fileBytes, "application/zip", Path.GetFileName(filePath));
		}
		
	}
}
