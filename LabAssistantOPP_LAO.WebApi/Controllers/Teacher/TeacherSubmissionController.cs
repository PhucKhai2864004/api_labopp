using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher.Enum;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
    [Route("api/teacher/submissions")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherSubmissionController : ControllerBase
    {
        private readonly ITeacherSubmissionService _service;

        public TeacherSubmissionController(ITeacherSubmissionService service)
        {
            _service = service;
        }

		[HttpGet("waiting/{classId}")]
		public async Task<IActionResult> GetWaitingReview(int classId, [FromQuery] SubmissionStatus? status)
		{
			var data = await _service.GetSubmissionsWaitingReviewAsync(classId, status);
			return Ok(ApiResponse<List<SubmissionDto>>.SuccessResponse(data, "Success"));
		}

		[HttpGet("{submissionId}")]
        public async Task<IActionResult> GetDetail(int submissionId)
        {
            var data = await _service.GetSubmissionDetailAsync(submissionId);
            return Ok(ApiResponse<SubmissionDetailDto>.SuccessResponse(data, "Success"));
        }

		[HttpPost("grade")]
		public async Task<IActionResult> Grade([FromBody] GradeSubmissionRequest request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ValidationErrorResponse());

			var ok = await _service.GradeSubmissionAsync(request.SubmissionId, request.Status);
			if (!ok)
				return BadRequest(ApiResponse<string>.ErrorResponse(
					"Submission not found or not in 'Submit' status"));

			return Ok(ApiResponse<string>.SuccessResponse("Graded"));
		}


		[HttpPost("feedback")]
        public async Task<IActionResult> Feedback([FromBody] FeedbackRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

			if (!int.TryParse(User.FindFirst("userId")?.Value, out int teacherId))
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được giáo viên"));
			}
			var ok = await _service.SubmitFeedbackAsync(request.SubmissionId, teacherId, request.Comment);
            return Ok(ApiResponse<string>.SuccessResponse(ok ? "Feedback submitted" : "Submission not found"));
        }

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
