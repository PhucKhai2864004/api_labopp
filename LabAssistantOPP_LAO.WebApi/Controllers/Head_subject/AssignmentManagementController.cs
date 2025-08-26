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
        private readonly LabOopChangeV6Context _context;

        public AssignmentManagementController(LabOopChangeV6Context context)
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
					TeacherId = a.TeacherId,
					Status = a.Status
				})
				.ToListAsync();

			return Ok(ApiResponse<List<LabAssignmentDto>>.SuccessResponse(assignments, "Danh sách đề bài"));
		}

		[HttpPost("add")]
		public async Task<IActionResult> AddAssignment([FromBody] CreateLabAssignmentDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ValidationErrorResponse());

			var validStatuses = new[] { "Pending", "Active", "Inactive" };
			if (!validStatuses.Contains(dto.Status))
				return BadRequest(ApiResponse<string>.ErrorResponse("Trạng thái không hợp lệ."));

			var assignment = new LabAssignment
			{
				Title = dto.Title,
				Description = dto.Description,
				LocTotal = dto.LocTotal ?? 0,
				TeacherId = dto.TeacherId,
				Status = dto.Status,
				CreatedAt = DateTime.Now,
				CreatedBy = dto.TeacherId
			};

			_context.LabAssignments.Add(assignment);
			await _context.SaveChangesAsync(); // Lưu để assignment có Id

			if (dto.ClassIds != null && dto.ClassIds.Any())
			{
				foreach (var classId in dto.ClassIds)
				{
					var classAssignment = new ClassHasLabAssignment
					{
						ClassId = classId,
						AssignmentId = assignment.Id
					};
					_context.ClassHasLabAssignments.Add(classAssignment);
				}
				await _context.SaveChangesAsync();
			}

			return Ok(ApiResponse<int>.SuccessResponse(assignment.Id, "Thêm đề bài thành công"));
		}


		// ✅ Sửa đề bài
		[HttpPut("update/{id}")]
		public async Task<IActionResult> UpdateAssignment(int id, [FromBody] LabAssignmentDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ValidationErrorResponse());

			var assignment = await _context.LabAssignments
										   .Include(a => a.ClassHasLabAssignments)
										   .FirstOrDefaultAsync(a => a.Id == id);

			if (assignment == null)
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy đề bài"));

			var validStatuses = new[] { "Pending", "Active", "Inactive" };
			if (!validStatuses.Contains(dto.Status))
				return BadRequest(ApiResponse<string>.ErrorResponse("Trạng thái không hợp lệ."));

			// Cập nhật thông tin cơ bản
			assignment.Title = dto.Title;
			assignment.Description = dto.Description;
			assignment.LocTotal = dto.LocTotal ?? 0;
			assignment.Status = dto.Status;
			assignment.UpdatedAt = DateTime.Now;
			assignment.UpdatedBy = dto.TeacherId;

			// Xử lý quan hệ với ClassHasLabAssignment
			if (dto.ClassIds != null)
			{
				// Xóa các quan hệ cũ
				_context.ClassHasLabAssignments.RemoveRange(assignment.ClassHasLabAssignments);

				// Thêm mới theo danh sách ClassIds
				foreach (var classId in dto.ClassIds)
				{
					var classAssignment = new ClassHasLabAssignment
					{
						AssignmentId = assignment.Id,
						ClassId = classId
					};
					_context.ClassHasLabAssignments.Add(classAssignment);
				}
			}

			await _context.SaveChangesAsync();

			return Ok(ApiResponse<int>.SuccessResponse(id, "Cập nhật đề bài thành công"));
		}


		// ✅ Xóa đề bài
		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> DeleteAssignment(int id)
		{
			var assignment = await _context.LabAssignments
				.Include(a => a.TestCases)
				.FirstOrDefaultAsync(a => a.Id == id);

			if (assignment == null)
				return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy đề bài"));

			// Xóa Class_Has_Lab_Assignment
			var classAssignments = _context.ClassHasLabAssignments.Where(ca => ca.AssignmentId == id);
			_context.ClassHasLabAssignments.RemoveRange(classAssignments);

			// Xóa Student_Lab_Assignment
			var studentAssignments = _context.StudentLabAssignments.Where(sa => sa.AssignmentId == id);
			_context.StudentLabAssignments.RemoveRange(studentAssignments);

			// Xóa TestCase liên quan
			_context.TestCases.RemoveRange(assignment.TestCases);

			// Xóa Document liên quan
			var docs = _context.AssignmentDocuments.Where(d => d.AssignmentId == id);
			_context.AssignmentDocuments.RemoveRange(docs);

			// Xóa assignment
			_context.LabAssignments.Remove(assignment);

			await _context.SaveChangesAsync();

			return Ok(ApiResponse<int>.SuccessResponse(id, "Xóa đề bài thành công"));
		}




		// Xem thống kê tất cả lớp
		[HttpGet("statistics/all")]
		public async Task<IActionResult> GetAllClassPassRates()
		{
			var allClasses = await _context.Classes.ToListAsync();
			var resultList = new List<ClassPassRateDto>();

			foreach (var classInfo in allClasses)
			{
				var studentIds = await _context.StudentInClasses
					.Where(sic => sic.ClassId == classInfo.Id)
					.Select(sic => sic.StudentId)
					.ToListAsync();

				int totalStudents = studentIds.Count;
				int studentsPassed = 0;

				foreach (var studentId in studentIds)
				{
					var totalLoc = await _context.StudentLabAssignments
						.Where(sla => sla.StudentId == studentId &&
									  _context.ClassHasLabAssignments
										  .Any(chla => chla.AssignmentId == sla.AssignmentId && chla.ClassId == classInfo.Id))
						.SumAsync(sla => (int?)sla.LocResult ?? 0);

					if (totalLoc >= 750)
					{
						studentsPassed++;
					}
				}

				resultList.Add(new ClassPassRateDto
				{
					ClassId = classInfo.Id,
					ClassName = classInfo.ClassCode,
					TotalStudents = totalStudents,
					StudentsPassed = studentsPassed,
					PassRate = totalStudents == 0 ? 0 : Math.Round((double)studentsPassed / totalStudents * 100, 2)
				});
			}

			return Ok(ApiResponse<List<ClassPassRateDto>>.SuccessResponse(resultList));
		}

		// Xem thống kê 1 lớp
		[HttpGet("statistics/{classId:int}")]
		public async Task<IActionResult> GetClassPassRate(int classId)
		{
			var classInfo = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId);
			if (classInfo == null)
				return NotFound(ApiResponse<string>.ErrorResponse("Class not found"));

			var studentIds = await _context.StudentInClasses
				.Where(sic => sic.ClassId == classId)
				.Select(sic => sic.StudentId)
				.ToListAsync();

			int totalStudents = studentIds.Count;
			int studentsPassed = 0;

			foreach (var studentId in studentIds)
			{
				var totalLoc = await _context.StudentLabAssignments
					.Where(sla => sla.StudentId == studentId &&
								  _context.ClassHasLabAssignments
									  .Any(chla => chla.AssignmentId == sla.AssignmentId && chla.ClassId == classId))
					.SumAsync(sla => (int?)sla.LocResult ?? 0);

				if (totalLoc >= 750)
				{
					studentsPassed++;
				}
			}

			var result = new ClassPassRateDto
			{
				ClassId = classId,
				ClassName = classInfo.ClassCode,
				TotalStudents = totalStudents,
				StudentsPassed = studentsPassed,
				PassRate = totalStudents == 0 ? 0 : Math.Round((double)studentsPassed / totalStudents * 100, 2)
			};

			return Ok(ApiResponse<ClassPassRateDto>.SuccessResponse(result));
		}

		// Export Excel
		[HttpGet("statistics/export-excel")]
		public async Task<IActionResult> ExportPassRateToExcel()
		{
			var classList = await _context.Classes.ToListAsync();
			var result = new List<(int ClassId, int Passed, int Total)>();

			foreach (var cls in classList)
			{
				var studentIds = await _context.StudentInClasses
					.Where(s => s.ClassId == cls.Id)
					.Select(s => s.StudentId)
					.ToListAsync();

				int total = studentIds.Count;
				int passed = 0;

				foreach (var studentId in studentIds)
				{
					var totalLoc = await _context.StudentLabAssignments
						.Where(sla => sla.StudentId == studentId &&
									  _context.ClassHasLabAssignments.Any(chla => chla.ClassId == cls.Id && chla.AssignmentId == sla.AssignmentId))
						.SumAsync(sla => (int?)sla.LocResult ?? 0);

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
		public async Task<IActionResult> UploadPdf(IFormFile file, [FromForm] int uploadedBy, [FromForm] int assignmentId)
		{
			if (file == null || file.Length == 0)
				return BadRequest(ApiResponse<object>.ErrorResponse("File không tồn tại"));

			if (file.ContentType != "application/pdf")
				return BadRequest(ApiResponse<object>.ErrorResponse("Chỉ hỗ trợ file PDF"));

			var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf");
			if (!Directory.Exists(uploadPath))
				Directory.CreateDirectory(uploadPath);

			var fileName = $"{Guid.NewGuid()}.pdf";
			var filePath = Path.Combine(uploadPath, fileName);

			// check xem assignment đã có file chưa
			var existingDoc = await _context.AssignmentDocuments
				.FirstOrDefaultAsync(d => d.AssignmentId == assignmentId);

			if (existingDoc != null)
			{
				// xóa file cũ nếu tồn tại
				var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingDoc.FilePath.TrimStart('/'));
				if (System.IO.File.Exists(oldPath))
					System.IO.File.Delete(oldPath);

				// ghi đè thông tin file mới
				using (var stream = new FileStream(filePath, FileMode.Create))
					await file.CopyToAsync(stream);

				existingDoc.FileName = file.FileName;
				existingDoc.FilePath = $"/uploads/pdf/{fileName}";
				existingDoc.MimeType = file.ContentType;
				existingDoc.UploadedBy = uploadedBy;
				existingDoc.UploadedAt = DateTime.Now;

				_context.AssignmentDocuments.Update(existingDoc);
				await _context.SaveChangesAsync();

				return Ok(ApiResponse<int>.SuccessResponse(existingDoc.Id, "Upload file thành công (ghi đè)"));
			}
			else
			{
				// tạo mới
				using (var stream = new FileStream(filePath, FileMode.Create))
					await file.CopyToAsync(stream);

				var doc = new AssignmentDocument
				{
					AssignmentId = assignmentId,
					FileName = file.FileName,
					FilePath = $"/uploads/pdf/{fileName}",
					MimeType = file.ContentType,
					UploadedBy = uploadedBy,
					UploadedAt = DateTime.Now
				};

				_context.AssignmentDocuments.Add(doc);
				await _context.SaveChangesAsync();

				return Ok(ApiResponse<int>.SuccessResponse(doc.Id, "Upload file thành công"));
			}
		}



		//[HttpPost]
		//      public async Task<IActionResult> AddPromt([FromBody] PromtCreateDto dto)
		//      {
		//          if (dto == null)
		//              return BadRequest("Invalid data.");

		//          var promt = new Promt
		//          {
		//              Id = Guid.NewGuid().ToString(), // tự sinh ID
		//              PromtDetail = dto.PromtDetail
		//          };

		//          _context.Promts.Add(promt);
		//          await _context.SaveChangesAsync();

		//          return Ok(promt);
		//      }

		// GET: api/classes
		[HttpGet("AllClasses")]
		public async Task<IActionResult> GetAllClasses()
		{
			var classes = await _context.Classes
				.Include(c => c.Semester) // lấy luôn thông tin Semester
				.Include(c => c.Teacher)  // lấy luôn thông tin Teacher
				.Include(c => c.ClassSlots) // lấy luôn các Slot
				.ToListAsync();

			return Ok(classes);
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
