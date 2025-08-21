using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
	[Route("api/teacher/students")]
	[ApiController]
	[Authorize(Roles = "Teacher")]
	public class TeacherStudentController : ControllerBase
	{
		private readonly ITeacherStudentService _service;
        private readonly LabOopChangeV6Context _context;
        public TeacherStudentController(ITeacherStudentService service, LabOopChangeV6Context context)
		{
			_service = service;
            _context = context;
        }

		[HttpGet("in-class/{classId}")]
		public async Task<IActionResult> GetStudents(int classId)
		{
			var data = await _service.GetStudentsInClassAsync(classId);
			return Ok(ApiResponse<List<StudentInClassDto>>.SuccessResponse(data, "Success"));
		}

		[HttpGet("{classId}/{studentId}")]
		public async Task<IActionResult> GetStudentDetail(int classId, int studentId)
		{
			var data = await _service.GetStudentDetailAsync(classId, studentId);
			return Ok(ApiResponse<StudentDetailDto>.SuccessResponse(data, "Success"));
		}

        [HttpGet("class/{classId}/students-progress")]
        [Authorize]
        public async Task<IActionResult> GetStudentsProgressByClass(int classId)
        {
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int teacherId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được giáo viên"));
			}

			// Kiểm tra lớp có thuộc giáo viên này không
			var classInfo = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (classInfo == null)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse("Bạn không có quyền xem lớp này"));
            }

            // Lấy danh sách studentId trong lớp
            var studentsInClass = await _context.StudentInClasses
                .Where(sic => sic.ClassId == classId)
                .Select(sic => sic.StudentId)
                .ToListAsync();

            // Lấy toàn bộ assignment của lớp
            var assignmentsInClass = await _context.ClassHasLabAssignments
                .Where(ca => ca.ClassId == classId)
                .Select(ca => ca.AssignmentId)
                .ToListAsync();

            // Lấy dữ liệu tiến độ cho từng sinh viên
            var progressList = await _context.Users
                .Where(u => studentsInClass.Contains(u.Id))
                .Select(u => new
                {
                    StudentId = u.Id,
                    StudentName = u.Name,
                    TotalLoc = _context.StudentLabAssignments
						.Where(s => s.StudentId == u.Id && s.LocResult != null)
                        .Sum(s => (int?)s.LocResult) ?? 0,
                    CompletedAssignments = _context.StudentLabAssignments
						.Where(s => s.StudentId == u.Id
                                 && s.Status == "Passed"
                                 && assignmentsInClass.Contains(s.AssignmentId))
                        .Select(s => s.AssignmentId)
                        .Distinct()
                        .Count(),
                    TotalAssignments = assignmentsInClass.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(progressList, "Tiến độ sinh viên trong lớp"));
        }

    }
}
