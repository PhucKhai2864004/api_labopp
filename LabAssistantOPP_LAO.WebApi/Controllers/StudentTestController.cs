using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers
{
	[ApiController]
	[Route("api/student")]
	public class StudentTestController : ControllerBase
	{
		// 🎯 Test token hợp lệ của Student
		[Authorize(Roles = "Student")]
		[HttpGet("profile")]
		public IActionResult GetStudentProfile()
		{
			var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
			var userId = User.FindFirst("userId")?.Value;

			return Ok(new
			{
				message = "Bạn đang đăng nhập với vai trò Student",
				email,
				userId
			});
		}

		// ❌ Test sai role → sẽ nhận 403 Forbidden nếu không phải Student
		[Authorize(Roles = "Teacher")]
		[HttpGet("teacher-only")]
		public IActionResult TeacherOnly()
		{
			return Ok("Bạn là Teacher (chỉ Teacher truy cập được).");
		}
	}
}
