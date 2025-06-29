using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
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
        public async Task<IActionResult> GetWaitingReview(string classId)
        {
            var data = await _service.GetSubmissionsWaitingReviewAsync(classId);
            return Ok(ApiResponse<List<SubmissionDto>>.SuccessResponse(data, "Success"));
        }

        [HttpGet("{submissionId}")]
        public async Task<IActionResult> GetDetail(string submissionId)
        {
            var data = await _service.GetSubmissionDetailAsync(submissionId);
            return Ok(ApiResponse<SubmissionDto>.SuccessResponse(data, "Success"));
        }

        [HttpPost("grade")]
        public async Task<IActionResult> Grade([FromBody] GradeSubmissionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var ok = await _service.GradeSubmissionAsync(request.SubmissionId, request.IsPass);
            return Ok(ApiResponse<string>.SuccessResponse(ok ? "Graded" : "Not Found"));
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> Feedback([FromBody] FeedbackRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var teacherId = User.FindFirstValue("userId");
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
