using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
    [Route("api/teacher/dashboard")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherDashboardController : ControllerBase
    {
        private readonly ITeacherDashboardService _dashboardService;
        private readonly LabOppContext _context;

        public TeacherDashboardController(ITeacherDashboardService dashboardService, LabOppContext context)
        {
            _dashboardService = dashboardService;
            _context = context;
        }

        [HttpGet("getDashboard/{classId}")]
        public async Task<IActionResult> GetDashboard(string classId)
        {
            var data = await _dashboardService.GetDashboardAsync(classId);
            return Ok(ApiResponse<TeacherDashboardDto>.SuccessResponse(data, "Success"));
        }

        [HttpGet("getClass/{teacherId}")]
        public async Task<IActionResult> GetManagedClasses(string teacherId)
        {
            var data = await _dashboardService.GetManagedClassesAsync(teacherId);
            return Ok(ApiResponse<List<ClassDto>>.SuccessResponse(data, "Success"));
        }

        [HttpGet("class-count")]
        public async Task<IActionResult> GetTeacherClassCount()
        {
            // Lấy userId từ JWT token
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy userId trong token"));
            }

            // Lấy tên teacher từ bảng User
            var teacher = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Name })
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy teacher"));
            }

            // Đếm số lớp teacher này dạy
            var classCount = await _context.Classes
                .CountAsync(c => c.TeacherId == userId);

            var result = new
            {
                TeacherName = teacher.Name,
                TotalClasses = classCount
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy dữ liệu thành công"));
        }

    }
}
