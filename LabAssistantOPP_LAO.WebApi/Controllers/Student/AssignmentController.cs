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

        private readonly LabOppContext _context;

        public AssignmentController(LabOppContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("student")]
        public async Task<IActionResult> GetAssignmentsForStudent()
        {
            var studentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(studentId))
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
                    (assignmentId, assignment) => new
                    {
                        assignment.Id,
                        assignment.Title,
                        assignment.Description,
                        assignment.LocTotal,
                        assignment.CreatedAt
                    }
                )
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(assignments, "Danh sách bài tập của bạn"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssignmentDetail(string id)
        {
            var assignment = await _context.LabAssignments
                .Where(a => a.Id == id)
                .Select(a => new AssignmentDetailDto
                {
                    Title = a.Title,
                    Description = a.Description,
                    LocTotal = a.LocTotal
                })
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy bài lab."));
            }

            return Ok(ApiResponse<AssignmentDetailDto>.SuccessResponse(assignment, "Lấy chi tiết bài lab thành công"));
        }

        [HttpPost("student-lab-assignment")]
        public async Task<IActionResult> AssignLabToStudent([FromBody] StudentLabAssignmentDto dto)
        {
            // Lấy student_id từ token
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không thể xác định sinh viên từ token"));
            }

            // Kiểm tra xem assignment đã được gán cho sinh viên chưa
            var existing = await _context.StudentLabAssignments
                .FirstOrDefaultAsync(sla => sla.AssignmentId == dto.AssignmentId && sla.StudentId == studentId);

            if (existing != null)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Bạn đã lấy assignment này rồi"));
            }

            // Tạo bản ghi mới
            var newRecord = new StudentLabAssignment
            {
                Id = Guid.NewGuid().ToString(),
                AssignmentId = dto.AssignmentId,
                StudentId = studentId
            };

            _context.StudentLabAssignments.Add(newRecord);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse(newRecord.Id, "Lấy assignment thành công"));
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAssignment([FromForm] SubmitAssignmentDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<string>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }
            var studentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
            }

            if (model.ZipFile == null || model.ZipFile.Length == 0)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("File không được để trống."));
            }

            if (Path.GetExtension(model.ZipFile.FileName).ToLower() != ".zip")
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Chỉ được phép nộp file .zip."));
            }

            // Check file size (limit to 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (model.ZipFile.Length > maxFileSize)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("File vượt quá kích thước tối đa là 10MB."));
            }

            // Lấy class_id mà student đang học assignment đó
            var classId = await (from s in _context.StudentInClasses
                                 join cha in _context.ClassHasLabAssignments on s.ClassId equals cha.ClassId
                                 where s.StudentId == studentId && cha.AssignmentId == model.AssignmentId
                                 select s.ClassId).FirstOrDefaultAsync();

            if (classId == null)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Không tìm thấy lớp học tương ứng với bài lab."));
            }

            // Đặt tên file: studentId_classId_assignmentId.zip
            var newFileName = $"{studentId}_{classId}_{model.AssignmentId}.zip";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "zips");
            Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, newFileName);

            // Ghi file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ZipFile.CopyToAsync(stream);
            }

            // Lưu vào bảng File
            var fileEntity = new UploadFile
            {
                Id = Guid.NewGuid().ToString(),
                OriginName = model.ZipFile.FileName,
                Name = newFileName,
                Path = Path.Combine("uploads", "zips", newFileName),
                MimeType = model.ZipFile.ContentType,
                Size = (int)model.ZipFile.Length,
                UploadedBy = studentId,
                UploadedAt = DateTime.Now
            };
            _context.Files.Add(fileEntity);

            // Kiểm tra submission
            var existing = await _context.Submissions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AssignmentId == model.AssignmentId);
            var status = model.Status?.ToLower();
            if (status != "draft" && status != "submit")
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Trạng thái không hợp lệ. Chỉ chấp nhận 'Draft' hoặc 'Submit'."));
            }
            if (existing != null)
            {
                existing.ZipCode = fileEntity.Id;
                existing.Status = model.Status;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedBy = studentId;
                existing.SubmittedAt = DateTime.Now;
            }
            else
            {
                var submission = new Submission
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    AssignmentId = model.AssignmentId,
                    ZipCode = fileEntity.Id,
                    Status = model.Status,
                    SubmittedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    CreatedBy = studentId
                };
                _context.Submissions.Add(submission);
            }

            await _context.SaveChangesAsync();


            var teacherId = await _context.StudentInClasses
                .Where(s => s.StudentId == studentId)
                .Join(
                    _context.Classes,
                    s => s.ClassId,
                    c => c.Id,
                    (s, c) => c.TeacherId
                )
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(teacherId))
            {
                await _hubContext.Clients
                    .User(teacherId)
                    .SendAsync("ReceiveNotification", new
                    {
                        message = $"Sinh viên {studentId} vừa nộp bài Assignment {model.AssignmentId}",
                        assignmentId = model.AssignmentId,
                        studentId = studentId,
                        submittedAt = DateTime.Now
                    });
            }


            return Ok(ApiResponse<string>.SuccessResponse("Thành công", "Đã nộp bài thành công."));
        }

        [HttpGet("my-submissions")]
        public async Task<IActionResult> ViewMySubmissions()
        {
            var studentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
            }

            var submissions = await _context.Submissions
                .Where(s => s.StudentId == studentId)
                .Include(s => s.ZipCodeNavigation) // lấy thông tin file
                .ToListAsync();

            var result = submissions.Select(s => new ViewMySubmissionDto
            {
                SubmissionId = s.Id,
                AssignmentId = s.AssignmentId,
                FileName = s.ZipCodeNavigation?.OriginName,
                FileUrl = s.ZipCodeNavigation != null ? $"/{s.ZipCodeNavigation.Path.Replace("\\", "/")}" : null,
                Status = s.Status,
                SubmittedAt = s.SubmittedAt ?? s.CreatedAt,
                LocResult = s.LocResult,
                ManuallyEdited = s.ManuallyEdited,
                ManualReason = s.ManualReason
            }).ToList();

            return Ok(ApiResponse<List<ViewMySubmissionDto>>.SuccessResponse(result, "Thành công"));

        }

        [HttpGet("my-total-loc")]
        public async Task<IActionResult> GetMyTotalLoc()
        {
            var studentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
            }

            var totalLoc = await _context.Submissions
                .Where(s => s.StudentId == studentId && s.LocResult != null)
                .SumAsync(s => s.LocResult.Value);

            return Ok(ApiResponse<int>.SuccessResponse(totalLoc, "Tổng LOC của bạn"));
        }

        [HttpGet("my-progress")]
        public async Task<IActionResult> GetMyProgress()
        {
            var studentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
            }

            // Tổng LOC
            var totalLoc = await _context.Submissions
                .Where(s => s.StudentId == studentId && s.LocResult != null)
                .SumAsync(s => s.LocResult.Value);

            // Tổng assignment của sinh viên
            var totalAssignments = await _context.StudentLabAssignments
                .Where(sla => sla.StudentId == studentId)
                .CountAsync();

            // Tổng bài submit PASS
            var passedAssignments = await _context.Submissions
                .Where(s => s.StudentId == studentId && s.Status == "Passed")
                .Select(s => s.AssignmentId)
                .Distinct()
                .CountAsync();

            // Xếp hạng theo LOC
            var studentLocs = await _context.Submissions
                .Where(s => s.LocResult != null)
                .GroupBy(s => s.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    TotalLoc = g.Sum(s => s.LocResult.Value)
                })
                .OrderByDescending(x => x.TotalLoc)
                .ToListAsync();

            var rank = studentLocs.FindIndex(s => s.StudentId == studentId) + 1;
            var totalStudents = studentLocs.Count;

            // Kết quả trả về
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
            // Lấy userId từ token (identity)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized"));
            }

            // Tìm tất cả lớp mà sinh viên này đang học
            var classIds = await _context.StudentInClasses
                .Where(sic => sic.StudentId == userId)
                .Select(sic => sic.ClassId)
                .ToListAsync();

            if (!classIds.Any())
            {
                return NotFound(ApiResponse<string>.ErrorResponse("You are not enrolled in any class"));
            }

            // Tìm tất cả sinh viên khác học chung các lớp đó
            var classmateIds = await _context.StudentInClasses
                .Where(sic => classIds.Contains(sic.ClassId) && sic.StudentId != userId)
                .Select(sic => sic.StudentId)
                .Distinct()
                .ToListAsync();

            // Truy vấn thông tin từ bảng User và Student
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
            var studentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));
            }

            var classes = await (from sic in _context.StudentInClasses
                                 join c in _context.Classes on sic.ClassId equals c.Id
                                 where sic.StudentId == studentId && c.IsActive == true
                                 select new
                                 {
                                     c.Id,
                                     c.Name,
                                     c.Subject,
                                     c.Semester,
                                     c.AcademicYear,
                                     c.LocToPass,
                                     c.TeacherId
                                 }).ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(classes, "Danh sách lớp của bạn"));
        }





    }
}
