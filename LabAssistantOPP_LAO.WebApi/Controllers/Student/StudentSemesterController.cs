using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Student
{
	[Authorize(Roles = "Student")]
	[ApiController]
	[Route("api/semester")]
	public class StudentSemesterController : ControllerBase
	{
		private readonly LabOopChangeV6Context _context;

		public StudentSemesterController(LabOopChangeV6Context context)
		{
			_context = context;
		}

		// GET: api/student/current-semester
		[HttpGet("current-semester")]
		public async Task<IActionResult> GetCurrentSemester()
		{
			var today = DateTime.UtcNow.Date;

			var semester = await _context.Semesters
				.FirstOrDefaultAsync(s => s.StartDate <= today && s.EndDate >= today);

			if (semester == null)
				return NotFound("No active semester found for today.");

			return Ok(new
			{
				semester.Id,
				semester.Name,
				semester.StartDate,
				semester.EndDate
			});
		}
	}
}
