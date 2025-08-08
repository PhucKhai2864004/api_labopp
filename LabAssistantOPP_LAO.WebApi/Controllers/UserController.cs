using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher.Enum;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabAssistantOPP_LAO.WebApi.Controllers
{
    [Route("api/user")]
    [ApiController]
    
    public class UserController : ControllerBase
    {
        private readonly LabOppContext _context;

        public UserController(LabOppContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        [Authorize] // bắt buộc phải đăng nhập
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized"));

            // Lấy thông tin user cơ bản
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<string>.ErrorResponse("User not found"));

            // Lấy role 
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Admin" || role == "Head_Subject")
            {
                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    user.Name,
                    user.Email,
                    Role = role
                }));
            }

            else if (role == "Teacher")
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == userId);
                if (teacher == null)
                    return NotFound(ApiResponse<string>.ErrorResponse("Teacher profile not found"));

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    user.Name,
                    user.Email,
                    Role = role,
                    teacher.TeacherCode,
                    teacher.AcademicTitle,
                    teacher.AcademicDegree,
                    teacher.DateOfBirth,
                    teacher.Phone,
                    teacher.Gender,
                    teacher.Address
                }));
            }

            else if (role == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == userId);
                if (student == null)
                    return NotFound(ApiResponse<string>.ErrorResponse("Student profile not found"));

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    user.Name,
                    user.Email,
                    Role = role,
                    student.StudentCode,
                    student.Major,
                    student.DateOfBirth,
                    student.Phone,
                    student.Gender,
                    student.Address
                }));
            }

            return BadRequest(ApiResponse<string>.ErrorResponse("Invalid role"));
        }



    }
}
