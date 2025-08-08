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
        private readonly LabOppContext _context;
        public TeacherAssignmentController(
    ITeacherAssignmentService service,
    IWebHostEnvironment environment,
    LabOppContext context)
        {
            _service = service;
            _environment = environment;
            _context = context;
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

        [HttpGet("view-submission")]
        public async Task<IActionResult> ViewJavaFileFromZip(string zipPath, string javaFileName)
        {
            if (!System.IO.File.Exists(zipPath))
                return NotFound("File zip không tồn tại.");

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(javaFileName));

            if (entry == null)
                return NotFound("Không tìm thấy file .java.");

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            return Content(content, "text/plain", Encoding.UTF8);
        }

        [HttpGet("view-java/{studentId}/{classId}/{assignmentId}")]
        public IActionResult ViewJavaFiles(string studentId, string classId, string assignmentId)
        {
            // Tên file nộp bài của sinh viên
            var fileName = $"{studentId}_{classId}_{assignmentId}.zip";
            var zipPath = Path.Combine("wwwroot", "uploads", "zips", fileName);


            if (!System.IO.File.Exists(zipPath))
                return NotFound("Không tìm thấy file zip");

            var result = new Dictionary<string, string>();

            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Bỏ qua thư mục và chỉ lấy file .java
                        if (!entry.FullName.EndsWith(".java", StringComparison.OrdinalIgnoreCase) || entry.Length == 0)
                            continue;

                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            var content = reader.ReadToEnd();
                            result[entry.FullName] = content;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi xử lý file zip: " + ex.Message);
            }

            return Ok(ApiResponse<Dictionary<string,string>>.SuccessResponse(result,"Return"));
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
