using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
	[Route("api/teacher/students")]
	[ApiController]
	[Authorize(Roles = "Teacher")]
	public class TeacherStudentController : ControllerBase
	{
		private readonly ITeacherStudentService _service;

		public TeacherStudentController(ITeacherStudentService service)
		{
			_service = service;
		}

		[HttpGet("in-class/{classId}")]
		public async Task<IActionResult> GetStudents(string classId)
		{
			var data = await _service.GetStudentsInClassAsync(classId);
			return Ok(ApiResponse<List<StudentInClassDto>>.SuccessResponse(data, "Success"));
		}

		[HttpGet("{classId}/{studentId}")]
		public async Task<IActionResult> GetStudentDetail(string classId, string studentId)
		{
			var data = await _service.GetStudentDetailAsync(classId, studentId);
			return Ok(ApiResponse<StudentDetailDto>.SuccessResponse(data, "Success"));
		}
	}
}
