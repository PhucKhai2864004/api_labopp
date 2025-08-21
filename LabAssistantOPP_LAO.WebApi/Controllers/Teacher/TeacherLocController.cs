using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
	[Route("api/teacher/loc")]
	[ApiController]
	[Authorize(Roles = "Teacher")]
	public class TeacherLocController : ControllerBase
	{
		private readonly ITeacherLocService _service;

		public TeacherLocController(ITeacherLocService service)
		{
			_service = service;
		}

		[HttpGet("ranking/{classId}")]
		public async Task<IActionResult> GetLocRanking(int classId)
		{
			var data = await _service.GetLocRankingAsync(classId);
			return Ok(ApiResponse<List<LocRankingDto>>.SuccessResponse(data, "Success"));
		}
	}
}
