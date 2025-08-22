using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
	[ApiController]
	[Route("api/teacher/class")]
	[Authorize(Roles = "Teacher")]
	public class ClassController : ControllerBase
	{
		private readonly LabOopChangeV6Context _context;

		public ClassController(LabOopChangeV6Context context)
		{
			_context = context;
		}

		// POST: api/teacher/class/start
		[HttpPost("start")]
		public async Task<IActionResult> StartClass(int classId)
		{
			var classEntity = await _context.Classes.FindAsync(classId);
			if (classEntity == null) return NotFound("Class not found.");

			// Lấy slot hiện tại theo thời gian
			var now = DateTime.UtcNow;
			var slot = await _context.ClassSlots
				.Where(s => s.ClassId == classId && s.StartTime <= now && s.EndTime >= now)
				.FirstOrDefaultAsync();

			if (slot == null) return BadRequest("No active slot at the moment.");

			slot.IsEnabled = true;

			_context.ClassSlotLogs.Add(new ClassSlotLog
			{
				ClassSlotId = slot.Id,
				ActorId = int.Parse(User.FindFirstValue("userId")!), // teacher id
				Action = "Enable",
				ActedAt = now
			});

			await _context.SaveChangesAsync();
			return Ok("Class started successfully.");
		}

		// POST: api/teacher/class/stop
		[HttpPost("stop")]
		public async Task<IActionResult> StopClass(int classId)
		{
			var classEntity = await _context.Classes.FindAsync(classId);
			if (classEntity == null) return NotFound("Class not found.");

			var slot = await _context.ClassSlots
				.Where(s => s.ClassId == classId && s.IsEnabled)
				.FirstOrDefaultAsync();

			if (slot == null) return BadRequest("No running slot found.");

			slot.IsEnabled = false;

			_context.ClassSlotLogs.Add(new ClassSlotLog
			{
				ClassSlotId = slot.Id,
				ActorId = int.Parse(User.FindFirstValue("userId")!),
				Action = "Disable",
				ActedAt = DateTime.UtcNow
			});

			await _context.SaveChangesAsync();
			return Ok("Class stopped successfully.");
		}

		// GET: api/teacher/class/status/{classId}
		[HttpGet("status/{classId}")]
		public async Task<IActionResult> GetClassStatus(int classId)
		{
			var now = DateTime.UtcNow;

			// Tìm slot hiện tại theo thời gian
			var slot = await _context.ClassSlots
				.Where(s => s.ClassId == classId && s.StartTime <= now && s.EndTime >= now)
				.FirstOrDefaultAsync();

			if (slot == null)
				return NotFound("Không có slot nào đang diễn ra cho class này.");

			return Ok(new
			{
				classId = classId,
				slotId = slot.Id,
				slotNo = slot.SlotNo,
				isEnabled = slot.IsEnabled, // ✅ đây là trạng thái mở/tắt
				startTime = slot.StartTime,
				endTime = slot.EndTime
			});
		}
	}
}
