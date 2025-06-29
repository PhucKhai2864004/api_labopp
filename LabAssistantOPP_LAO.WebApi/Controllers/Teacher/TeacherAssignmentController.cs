using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
    [Route("api/teacher/assignments")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherAssignmentController : ControllerBase
    {
        private readonly ITeacherAssignmentService _service;

        public TeacherAssignmentController(ITeacherAssignmentService service)
        {
            _service = service;
        }

        [HttpGet("{classId}")]
        public async Task<IActionResult> GetAssignments(string classId)
        {
            var data = await _service.GetAssignmentsByClassAsync(classId);
            return Ok(ApiResponse<List<AssignmentDto>>.SuccessResponse(data, "Success"));
        }

        [HttpGet("detail/{assignmentId}")]
        public async Task<IActionResult> GetAssignment(string assignmentId)
        {
            var data = await _service.GetAssignmentDetailAsync(assignmentId);
            return Ok(ApiResponse<AssignmentDto>.SuccessResponse(data, "Success"));
        }

        [HttpPost("{classId}")]
        public async Task<IActionResult> CreateAssignment(string classId, [FromBody] CreateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var teacherId = User.FindFirstValue("userId");
            var newId = await _service.CreateAssignmentAsync(classId, teacherId, request);
            return Ok(ApiResponse<string>.SuccessResponse(newId, "Created"));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAssignment([FromBody] UpdateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var success = await _service.UpdateAssignmentAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(success ? "Updated" : "Not found"));
        }

        // 🔁 Reusable method for extracting validation error messages
        private ApiResponse<string> ValidationErrorResponse()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return ApiResponse<string>.ErrorResponse("Invalid input", errors);
        }
    }
}
