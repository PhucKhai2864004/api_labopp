using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Teacher
{
    [Route("api/teacher/assignments")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherAssignmentController : ControllerBase
    {
        private readonly ITeacherAssignmentService _service;
        private readonly IWebHostEnvironment _environment;
        private readonly LabOopChangeV6Context _context;
        public TeacherAssignmentController(
    ITeacherAssignmentService service,
    IWebHostEnvironment environment,
    LabOopChangeV6Context context)
        {
            _service = service;
            _environment = environment;
            _context = context;
        }

        [HttpGet("{classId}")]
        public async Task<IActionResult> GetAssignments(int classId)
        {
            var data = await _service.GetAssignmentsByClassAsync(classId);
            return Ok(ApiResponse<List<AssignmentDto>>.SuccessResponse(data, "Success"));
        }

        [HttpGet("detail/{assignmentId}")]
        public async Task<IActionResult> GetAssignment(int assignmentId)
        {
            var data = await _service.GetAssignmentDetailAsync(assignmentId);
            return Ok(ApiResponse<AssignmentDto>.SuccessResponse(data, "Success"));
        }

        [HttpPost("{classId}")]
        public async Task<IActionResult> CreateAssignment(int classId, [FromBody] CreateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

			if (!int.TryParse(User.FindFirstValue("userId"), out int teacherId))
				return Unauthorized(ApiResponse<string>.ErrorResponse("Không xác định được giáo viên"));

			var newId = await _service.CreateAssignmentAsync(classId, teacherId, request);

			return Ok(ApiResponse<int>.SuccessResponse(newId, "Created"));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAssignment([FromBody] UpdateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var success = await _service.UpdateAssignmentAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(success ? "Updated" : "Not found"));
        }

        //[HttpGet("view-submission")]
        //public async Task<IActionResult> ViewJavaFileFromZip(string zipPath, string javaFileName)
        //{
        //    if (!System.IO.File.Exists(zipPath))
        //        return NotFound("File zip không tồn tại.");

        //    using var archive = ZipFile.OpenRead(zipPath);
        //    var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(javaFileName));

        //    if (entry == null)
        //        return NotFound("Không tìm thấy file .java.");

        //    using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();

        //    return Content(content, "text/plain", Encoding.UTF8);
        //}

		[HttpGet("view-java/{submissionId}")]
		public async Task<IActionResult> ViewJavaFiles(int submissionId)
		{
			var sla = await _context.StudentLabAssignments.FindAsync(submissionId);
			if (sla == null || string.IsNullOrEmpty(sla.SubmissionZip))
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy submission"));

			var zipPath = Path.Combine("wwwroot", sla.SubmissionZip.Replace("/", Path.DirectorySeparatorChar.ToString()));

			if (!System.IO.File.Exists(zipPath))
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy file zip"));

			var result = new Dictionary<string, string>();

			try
			{
				using (var archive = ZipFile.OpenRead(zipPath))
				{
					foreach (var entry in archive.Entries)
					{
						if (!entry.FullName.EndsWith(".java", StringComparison.OrdinalIgnoreCase) || entry.Length == 0)
							continue;

						using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
						{
							var content = await reader.ReadToEndAsync();
							result[entry.FullName] = content;
						}
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<string>.ErrorResponse("Lỗi xử lý file zip: " + ex.Message));
			}

			return Ok(ApiResponse<Dictionary<string, string>>.SuccessResponse(result, "Danh sách file .java và nội dung"));
		}


		[HttpGet("list-java/{submissionId}")]
		public async Task<IActionResult> ListJavaFiles(int submissionId)
		{
			var sla = await _context.StudentLabAssignments.FindAsync(submissionId);
			if (sla == null || string.IsNullOrEmpty(sla.SubmissionZip))
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy submission"));

			// Ghép wwwroot + path trong DB
			var zipPath = Path.Combine("wwwroot", sla.SubmissionZip.Replace("/", Path.DirectorySeparatorChar.ToString()));

			if (!System.IO.File.Exists(zipPath))
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy file zip"));

			var javaFiles = new List<string>();

			try
			{
				using (var archive = ZipFile.OpenRead(zipPath))
				{
					foreach (var entry in archive.Entries)
					{
						if (entry.FullName.EndsWith(".java", StringComparison.OrdinalIgnoreCase) && entry.Length > 0)
						{
							javaFiles.Add(entry.FullName);
						}
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<string>.ErrorResponse("Lỗi xử lý file zip: " + ex.Message));
			}

			return Ok(ApiResponse<List<string>>.SuccessResponse(javaFiles, "Danh sách file .java"));
		}



		//Lấy nội dung của 1 file .java
		[HttpGet("java-content/{submissionId}")]
		public async Task<IActionResult> GetJavaFileContent(int submissionId, [FromQuery] string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return BadRequest(ApiResponse<string>.ErrorResponse("Thiếu tên file"));

			var sla = await _context.StudentLabAssignments.FindAsync(submissionId);
			if (sla == null || string.IsNullOrEmpty(sla.SubmissionZip))
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy submission"));

			var zipPath = Path.Combine("wwwroot", sla.SubmissionZip.Replace("/", Path.DirectorySeparatorChar.ToString()));

			if (!System.IO.File.Exists(zipPath))
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy file zip"));

			try
			{
				using (var archive = ZipFile.OpenRead(zipPath))
				{
					var entry = archive.Entries.FirstOrDefault(e =>
						e.FullName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

					if (entry == null)
						return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy file trong zip"));

					using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
					{
						var content = await reader.ReadToEndAsync();
						return Ok(ApiResponse<string>.SuccessResponse(content, "Nội dung file"));
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<string>.ErrorResponse("Lỗi xử lý file zip: " + ex.Message));
			}
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
