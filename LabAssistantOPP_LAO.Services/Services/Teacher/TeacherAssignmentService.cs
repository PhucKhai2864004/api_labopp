using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services.Teacher
{
	public class TeacherAssignmentService : ITeacherAssignmentService
	{
		private readonly LabOopChangeV6Context _context;

		public TeacherAssignmentService(LabOopChangeV6Context context)
		{
			_context = context;
		}

		public async Task<List<AssignmentDto>> GetAssignmentsByClassAsync(int classId)
		{
			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var submissions = await _context.StudentLabAssignments
				.Where(s => assignmentIds.Contains(s.AssignmentId))
				.ToListAsync();

			var assignments = await _context.LabAssignments
				.Where(a => assignmentIds.Contains(a.Id))
				.OrderByDescending(a => a.UpdatedAt)
				.ToListAsync();

			var result = assignments.Select(a => new AssignmentDto
			{
				Id = a.Id,
				Title = a.Title,
				Description = a.Description,
				LocTarget = a.LocTotal ?? 0,
				Status = "Open", // TODO: update logic later
				TotalSubmissions = submissions.Count(s => s.AssignmentId == a.Id),
				PassedCount = submissions.Count(s => s.AssignmentId == a.Id && s.Status == "Passed")
			}).ToList();

			return result;
		}


		public async Task<AssignmentDto?> GetAssignmentDetailAsync(int assignmentId)
		{
			var assignment = await _context.LabAssignments.FindAsync(assignmentId);
			if (assignment == null) return null;

			var totalSub = await _context.StudentLabAssignments.CountAsync(s => s.AssignmentId == assignmentId);
			var passed = await _context.StudentLabAssignments.CountAsync(s => s.AssignmentId == assignmentId && s.Status == "Passed");

			return new AssignmentDto
			{
				Id = assignment.Id,
				Title = assignment.Title,
				Description = assignment.Description,
				LocTarget = assignment.LocTotal ?? 0,
				Status = "Open",
				TotalSubmissions = totalSub,
				PassedCount = passed
			};
		}


		public async Task<int> CreateAssignmentAsync(int classId, int teacherId, CreateAssignmentRequest request)
		{
			var assignment = new LabAssignment
			{
				Title = request.Title,
				Description = request.Description,
				LocTotal = request.LocTarget,
				TeacherId = teacherId,
				CreatedBy = teacherId,
				CreatedAt = DateTime.UtcNow,
				UpdatedBy = teacherId,
				UpdatedAt = DateTime.UtcNow
			};

			await _context.LabAssignments.AddAsync(assignment);
			await _context.SaveChangesAsync(); // cần assignment.Id

			// Mapping class
			var mapping = new ClassHasLabAssignment
			{
				ClassId = classId,
				AssignmentId = assignment.Id
			};
			await _context.ClassHasLabAssignments.AddAsync(mapping);
			await _context.SaveChangesAsync();

			// Upload file PDF nếu có
			if (request.File != null && request.File.Length > 0)
			{
				if (request.File.ContentType != "application/pdf")
					throw new InvalidOperationException("Only PDF files are allowed");

				var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf");
				if (!Directory.Exists(uploadPath))
					Directory.CreateDirectory(uploadPath);

				var fileName = $"{Guid.NewGuid()}.pdf";
				var filePath = Path.Combine(uploadPath, fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
					await request.File.CopyToAsync(stream);

				var doc = new AssignmentDocument
				{
					AssignmentId = assignment.Id,
					FileName = request.File.FileName,
					FilePath = $"/uploads/pdf/{fileName}",
					MimeType = request.File.ContentType,
					UploadedBy = teacherId,
					UploadedAt = DateTime.UtcNow
				};

				await _context.AssignmentDocuments.AddAsync(doc);
				await _context.SaveChangesAsync();
			}

			return assignment.Id;
		}

		public async Task<bool> UpdateAssignmentAsync(int teacherId, UpdateAssignmentRequest request)
		{
			var assignment = await _context.LabAssignments.FindAsync(request.AssignmentId);
			if (assignment == null) return false;

			// Chỉ cho phép update nếu Status = Pending
			if (!string.Equals(assignment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
				return false;

			assignment.Title = request.Title;
			assignment.Description = request.Description;
			assignment.LocTotal = request.LocTarget;
			assignment.UpdatedAt = DateTime.UtcNow;
			assignment.UpdatedBy = teacherId;

			// Upload file PDF nếu có
			if (request.File != null && request.File.Length > 0)
			{
				if (request.File.ContentType != "application/pdf")
					throw new InvalidOperationException("Only PDF files are allowed");

				var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pdf");
				if (!Directory.Exists(uploadPath))
					Directory.CreateDirectory(uploadPath);

				var fileName = $"{Guid.NewGuid()}.pdf";
				var filePath = Path.Combine(uploadPath, fileName);

				var existingDoc = await _context.AssignmentDocuments
					.FirstOrDefaultAsync(d => d.AssignmentId == assignment.Id);

				if (existingDoc != null)
				{
					// Xóa file cũ
					var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingDoc.FilePath.TrimStart('/'));
					if (System.IO.File.Exists(oldPath))
						System.IO.File.Delete(oldPath);

					// Ghi đè file mới
					using (var stream = new FileStream(filePath, FileMode.Create))
						await request.File.CopyToAsync(stream);

					existingDoc.FileName = request.File.FileName;
					existingDoc.FilePath = $"/uploads/pdf/{fileName}";
					existingDoc.MimeType = request.File.ContentType;
					existingDoc.UploadedBy = teacherId;
					existingDoc.UploadedAt = DateTime.UtcNow;

					_context.AssignmentDocuments.Update(existingDoc);
				}
				else
				{
					// Tạo mới
					using (var stream = new FileStream(filePath, FileMode.Create))
						await request.File.CopyToAsync(stream);

					var doc = new AssignmentDocument
					{
						AssignmentId = assignment.Id,
						FileName = request.File.FileName,
						FilePath = $"/uploads/pdf/{fileName}",
						MimeType = request.File.ContentType,
						UploadedBy = teacherId,
						UploadedAt = DateTime.UtcNow
					};

					await _context.AssignmentDocuments.AddAsync(doc);
				}
			}

			await _context.SaveChangesAsync();
			return true;
		}


	}
}
