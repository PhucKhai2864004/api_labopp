using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LabAssistantOPP_LAO.WebApi.Controllers.Head_subject
{
    [Authorize(Roles = "Head Subject")]
    [ApiController]
    [Route("api/head_subject/semester")]
    public class SemesterController : ControllerBase
    {
        private readonly LabOppContext _context;
        private static (int Semester, string AcademicYear)? _currentSemester = null;

        public SemesterController(LabOppContext context)
        {
            _context = context;
        }

        // ✅ 1. Xem danh sách học kỳ
        [HttpGet("semester")]
        public async Task<IActionResult> GetSemesters()
        {
            var semesters = await _context.Classes
                .OrderByDescending(c => c.AcademicYear)
                .ThenByDescending(c => c.Semester)
                .Select(c => new
                {
                    c.Id,
                    Name = c.Name,
                    Semester = c.Semester,
                    AcademicYear = c.AcademicYear,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(semesters));
        }


        [HttpPost("class")]
        public async Task<IActionResult> AddClass([FromBody] AddClassRequestDto request)
        {
            try
            {
                var newClass = new Class
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    Subject = request.Subject,
                    Semester = request.Semester,
                    AcademicYear = request.AcademicYear,
                    LocToPass = request.LocToPass,
                    TeacherId = request.TeacherId,
                    IsActive = request.IsActive,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Classes.Add(newClass);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.SuccessResponse(newClass.Id, "Thêm lớp học thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Thêm lớp học thất bại", new List<string> { ex.Message }));
            }
        }
        
    }

}
