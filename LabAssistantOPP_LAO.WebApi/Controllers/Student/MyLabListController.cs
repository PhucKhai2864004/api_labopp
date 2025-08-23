using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Student
{
	[Route("api/student/my-lab-list")]
	[ApiController]
	[Authorize(Roles = "Student")]
	public class MyLabListController : ControllerBase
	{
		private readonly LabOopChangeV6Context _context;

		public MyLabListController(LabOopChangeV6Context context)
		{
			_context = context;
		}

		// GET: Lấy danh sách bài đang làm của sinh viên
		[HttpGet]
		public async Task<IActionResult> GetMyLabList()
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));

			var myLabs = await _context.StudentLabAssignments
				.Where(sla => sla.StudentId == studentId)
				.Select(sla => new
				{
					sla.Id,
					sla.AssignmentId,
					sla.SemesterId,
					sla.Status,
					sla.SubmittedAt
				}).ToListAsync();

			return Ok(ApiResponse<object>.SuccessResponse(myLabs, "Danh sách bài đang làm của bạn"));
		}

		// POST: Thêm bài vào danh sách
		[HttpPost("{assignmentId:int}")]
		public async Task<IActionResult> AddLabToMyList(int assignmentId, [FromQuery] int semesterId)
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));

			// Kiểm tra xem bài đã được thêm chưa
			var existing = await _context.StudentLabAssignments
				.FirstOrDefaultAsync(sla => sla.StudentId == studentId
										 && sla.AssignmentId == assignmentId
										 && sla.SemesterId == semesterId);

			if (existing != null)
				return BadRequest(ApiResponse<string>.ErrorResponse("Bài này bạn đã chọn rồi"));

			var newSla = new StudentLabAssignment
			{
				StudentId = studentId,
				AssignmentId = assignmentId,
				SemesterId = semesterId,
				Status = "Draft"
			};

			_context.StudentLabAssignments.Add(newSla);
			await _context.SaveChangesAsync();

			return Ok(ApiResponse<int>.SuccessResponse(newSla.Id, "Thêm bài thành công"));
		}

		// DELETE: Xóa bài khỏi danh sách
		[HttpDelete("{assignmentId:int}")]
		public async Task<IActionResult> RemoveLabFromMyList(int assignmentId, [FromQuery] int semesterId)
		{
			if (!int.TryParse(User.FindFirst("userId")?.Value, out int studentId))
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được sinh viên"));

			var existing = await _context.StudentLabAssignments
				.FirstOrDefaultAsync(sla => sla.StudentId == studentId
										 && sla.AssignmentId == assignmentId
										 && sla.SemesterId == semesterId);

			if (existing == null)
				return NotFound(ApiResponse<string>.ErrorResponse("Bài này không tồn tại trong danh sách của bạn"));

			_context.StudentLabAssignments.Remove(existing);
			await _context.SaveChangesAsync();

			return Ok(ApiResponse<string>.SuccessResponse("Xóa bài thành công"));
		}
	}
}
