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
        private readonly LabOopChangeV6Context _context;
        private static (int Semester, string AcademicYear)? _currentSemester = null;

        public SemesterController(LabOopChangeV6Context context)
        {
            _context = context;
        }

        // ✅ 1. Xem danh sách học kỳ
        [HttpGet("list")]
        public async Task<IActionResult> GetSemesters()
        {
            var semesters = await _context.Semesters
                .OrderByDescending(s => s.StartDate)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.StartDate,
                    s.EndDate
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(semesters, "Lấy danh sách học kỳ thành công"));
        }

		[HttpPost("add")]
		public async Task<IActionResult> AddSemester([FromBody] Semester request)
		{
			try
			{
				var newSemester = new Semester
				{
					Name = request.Name,
					StartDate = request.StartDate,
					EndDate = request.EndDate
				};

				_context.Semesters.Add(newSemester);
				await _context.SaveChangesAsync();

				return Ok(ApiResponse<int>.SuccessResponse(newSemester.Id, "Thêm học kỳ thành công"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.ErrorResponse("Thêm học kỳ thất bại", new List<string> { ex.Message }));
			}
		}


		[HttpGet("{semesterId}/classes")]
		public async Task<IActionResult> GetClassesBySemester(int semesterId)
		{
			try
			{
				var semester = await _context.Semesters.FindAsync(semesterId);
				if (semester == null)
				{
					return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy học kỳ"));
				}

				var classes = await _context.Classes
					.Where(c => c.SemesterId == semesterId)
					.Select(c => new
					{
						c.Id,
						c.ClassCode,
						c.SubjectCode,
						c.AcademicYear,
						c.IsActive,
						c.LocToPass,
						c.TeacherId
					})
					.ToListAsync();

				return Ok(ApiResponse<object>.SuccessResponse(classes, "Lấy danh sách lớp theo học kỳ thành công"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.ErrorResponse("Không thể lấy danh sách lớp", new List<string> { ex.Message }));
			}
		}

		[HttpPost("{semesterId}/add-class")]
		public async Task<IActionResult> AddClass(int semesterId, [FromBody] AddClassRequestDto request)
		{
			try
			{
				var semester = await _context.Semesters.FindAsync(semesterId);
				if (semester == null)
				{
					return NotFound(ApiResponse<string>.ErrorResponse("Học kỳ không tồn tại"));
				}

				var newClass = new Class
				{
					ClassCode = request.ClassCode,
					SubjectCode = request.Subject,
					SemesterId = semesterId,
					AcademicYear = request.AcademicYear,
					IsActive = request.IsActive,
					LocToPass = request.LocToPass,
					TeacherId = request.TeacherId
				};

				_context.Classes.Add(newClass);
				await _context.SaveChangesAsync();

				return Ok(ApiResponse<int>.SuccessResponse(newClass.Id, "Thêm lớp học thành công"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.ErrorResponse("Thêm lớp học thất bại", new List<string> { ex.Message }));
			}
		}

	}

}
