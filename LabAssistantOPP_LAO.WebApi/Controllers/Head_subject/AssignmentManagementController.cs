using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Head_subject

{
	[Authorize(Roles = "Head Subject")]
    [ApiController]
	[Route("api/head_subject/assignment")]
    public class AssignmentManagementController : ControllerBase
    {
        private readonly LabOppContext _context;

        public AssignmentManagementController(LabOppContext context)
        {
            _context = context;
        }

        // ✅ Xem danh sách đề bài
        [HttpGet("list")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var assignments = await _context.LabAssignments
                .Select(a => new LabAssignmentDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    LocTotal = a.LocTotal,
                    TeacherId = a.TeacherId
                })
                .ToListAsync();

            return Ok(ApiResponse<List<LabAssignmentDto>>.SuccessResponse(assignments, "Danh sách đề bài"));
        }

        // ✅ Thêm đề bài
        [HttpPost("add")]
        public async Task<IActionResult> AddAssignment([FromBody] LabAssignmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());
            var newId = Guid.NewGuid().ToString();

            // Validate status
            var validStatuses = new[] { "Pending", "Active", "Inactive" };
            if (!validStatuses.Contains(dto.Status))
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Trạng thái không hợp lệ."));
            }

            var assignment = new LabAssignment
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                LocTotal = dto.LocTotal ?? 0,
                TeacherId = dto.TeacherId,
                Status = dto.Status,
                CreatedAt = DateTime.Now,
                CreatedBy = dto.TeacherId
            };

            _context.LabAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse(newId, "Thêm đề bài thành công"));
        }


        // ✅ Sửa đề bài
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateAssignment(string id, [FromBody] LabAssignmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());
            var assignment = await _context.LabAssignments.FindAsync(id);
            if (assignment == null)
                return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy đề bài"));

            // Validate status
            var validStatuses = new[] { "Pending", "Active", "Inactive" };
            if (!validStatuses.Contains(dto.Status))
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Trạng thái không hợp lệ."));
            }

            assignment.Title = dto.Title;
            assignment.Description = dto.Description;
            assignment.LocTotal = dto.LocTotal ?? 0;
            assignment.Status = dto.Status;
            assignment.UpdatedAt = DateTime.Now;
            assignment.UpdatedBy = dto.TeacherId;

            _context.LabAssignments.Update(assignment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse(id, "Cập nhật đề bài thành công"));
        }


        // ✅ Xóa đề bài
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteAssignment(string id)
        {
            var assignment = await _context.LabAssignments.FindAsync(id);
            if (assignment == null)
                return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy đề bài"));

            _context.LabAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse(id, "Xóa đề bài thành công"));
        }



        [HttpGet("statistics/all")]
        public async Task<IActionResult> GetAllClassPassRates()
        {
            var allClasses = await _context.Classes.ToListAsync();
            var resultList = new List<ClassPassRateDto>();

            foreach (var classInfo in allClasses)
            {
                var students = await _context.StudentInClasses
                    .Where(sic => sic.ClassId == classInfo.Id)
                    .Select(sic => sic.Student)
                    .ToListAsync();

                int totalStudents = students.Count;
                int studentsPassed = 0;

                foreach (var student in students)
                {
                    var totalLoc = await _context.Submissions
                        .Where(s => s.StudentId == student.Id &&
                                    _context.LabAssignments
                                        .Join(_context.ClassHasLabAssignments,
                                              la => la.Id,
                                              chla => chla.AssignmentId,
                                              (la, chla) => new { la, chla })
                                        .Any(joined => joined.la.Id == s.AssignmentId && joined.chla.ClassId == classInfo.Id)
                              )
                        .SumAsync(s => (int?)s.LocResult ?? 0);

                    if (totalLoc >= 750)
                    {
                        studentsPassed++;
                    }
                }

                var result = new ClassPassRateDto
                {
                    ClassId = classInfo.Id,
                    ClassName = classInfo.Name,
                    TotalStudents = totalStudents,
                    StudentsPassed = studentsPassed,
                    PassRate = totalStudents == 0 ? 0 : Math.Round((double)studentsPassed / totalStudents * 100, 2)
                };

                resultList.Add(result);
            }

            return Ok(ApiResponse<List<ClassPassRateDto>>.SuccessResponse(resultList));
        }

        //Xem thống kê tỉ lệ pass
        [HttpGet("statistics/{classId}")]
        public async Task<IActionResult> GetClassPassRate(string classId)
        {
            var classInfo = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId);
            if (classInfo == null)
            {
                return NotFound(ApiResponse<string>.ErrorResponse("Class not found"));
            }

            var students = await _context.StudentInClasses
                .Where(sic => sic.ClassId == classId)
                .Select(sic => sic.Student)
                .ToListAsync();

            int totalStudents = students.Count;
            int studentsPassed = 0;

            foreach (var student in students)
            {
                var totalLoc = await _context.Submissions
                    .Where(s => s.StudentId == student.Id &&
                                _context.LabAssignments
                                    .Join(_context.ClassHasLabAssignments,
                                          la => la.Id,
                                          chla => chla.AssignmentId,
                                          (la, chla) => new { la, chla })
                                    .Any(joined => joined.la.Id == s.AssignmentId && joined.chla.ClassId == classId)
                          )
                    .SumAsync(s => (int?)s.LocResult ?? 0);

                if (totalLoc >= 750)
                {
                    studentsPassed++;
                }
            }

            var result = new ClassPassRateDto
            {
                ClassId = classId,
                ClassName = classInfo.Name,
                TotalStudents = totalStudents,
                StudentsPassed = studentsPassed,
                PassRate = totalStudents == 0 ? 0 : Math.Round((double)studentsPassed / totalStudents * 100, 2)
            };

            return Ok(ApiResponse<ClassPassRateDto>.SuccessResponse(result));
        }


        [HttpGet("statistics/export-excel")]
        public async Task<IActionResult> ExportPassRateToExcel()
        {
            var classList = await _context.Classes.ToListAsync();
            var result = new List<(string ClassId, int Passed, int Total)>();

            foreach (var cls in classList)
            {
                var students = await _context.StudentInClasses
                    .Where(s => s.ClassId == cls.Id)
                    .Select(s => s.StudentId)
                    .ToListAsync();

                int total = students.Count;
                int passed = 0;

                foreach (var studentId in students)
                {
                    var totalLoc = await _context.Submissions
                        .Where(s => s.StudentId == studentId &&
                                    _context.ClassHasLabAssignments.Any(c => c.ClassId == cls.Id && c.AssignmentId == s.AssignmentId))
                        .SumAsync(s => (int?)s.LocResult ?? 0);

                    if (totalLoc >= 750)
                        passed++;
                }

                result.Add((cls.Id, passed, total));
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; 

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Pass Rate");

            // Header
            worksheet.Cells[1, 1].Value = "ClassID";
            worksheet.Cells[1, 2].Value = "Tỉ lệ pass";
            worksheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;

            // Data
            for (int i = 0; i < result.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = result[i].ClassId;
                worksheet.Cells[i + 2, 2].Value = $"{result[i].Passed}/{result[i].Total}";
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream(package.GetAsByteArray());
            string fileName = $"pass_rate_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


        [HttpPost("pdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file, [FromForm] string uploadedBy)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không tồn tại");

            if (file.ContentType != "application/pdf")
                return BadRequest("Chỉ hỗ trợ file PDF");

            var fileId = Guid.NewGuid().ToString();
            var fileName = $"{fileId}.pdf";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var uploadFile = new UploadFile
            {
                Id = fileId,
                OriginName = file.FileName,
                Name = fileName,
                Path = $"/uploads/pdf/{fileName}",
                MimeType = file.ContentType,
                Size = (int)file.Length,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            };

            _context.Files.Add(uploadFile);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tải file thành công",
                file = uploadFile,
                id = uploadFile.Id
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddPromt([FromBody] PromtCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data.");

            var promt = new Promt
            {
                Id = Guid.NewGuid().ToString(), // tự sinh ID
                PromtDetail = dto.PromtDetail
            };

            _context.Promts.Add(promt);
            await _context.SaveChangesAsync();

            return Ok(promt);
        }


        private ApiResponse<string> ValidationErrorResponse()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return ApiResponse<string>.ErrorResponse("Dữ liệu không hợp lệ", errors);
        }

    }

}
