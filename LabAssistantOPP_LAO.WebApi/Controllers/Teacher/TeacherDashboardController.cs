using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
	[Route("api/teacher/dashboard")]
	[ApiController]
	[Authorize(Roles = "Teacher")]
	public class TeacherDashboardController : ControllerBase
	{
		private readonly ITeacherDashboardService _dashboardService;

		public TeacherDashboardController(ITeacherDashboardService dashboardService)
		{
			_dashboardService = dashboardService;
		}

		[HttpGet("{classId}")]
		public async Task<IActionResult> GetDashboard(string classId)
		{
			var data = await _dashboardService.GetDashboardAsync(classId);
			return Ok(ApiResponse<TeacherDashboardDto>.SuccessResponse(data, "Success"));
		}
	}
}
