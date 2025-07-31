using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Entities;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Student
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class AssignmentController : ControllerBase
    {
        private readonly LabOppContext _context;

        public AssignmentController(LabOppContext context)
        {
            _context = context;
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

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAssignment([FromForm] SubmitAssignmentDto model)
        {
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

            if (existing != null)
            {
                existing.ZipCode = fileEntity.Id;
                existing.Status = "Draft";
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
                    Status = "Draft",
                    SubmittedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    CreatedBy = studentId
                };
                _context.Submissions.Add(submission);
            }

            await _context.SaveChangesAsync();

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

        



    }
}
