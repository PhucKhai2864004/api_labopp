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
		private readonly LabOppContext _context;

		public TeacherAssignmentService(LabOppContext context)
		{
			_context = context;
		}

		public async Task<List<AssignmentDto>> GetAssignmentsByClassAsync(string classId)
		{
			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var submissions = await _context.Submissions
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
				LocTarget = (int)a.LocTotal,
				Status = "Open", // TODO: update logic later
				TotalSubmissions = submissions.Count(s => s.AssignmentId == a.Id),
				PassedCount = submissions.Count(s => s.AssignmentId == a.Id && s.Status == "Passed")
			}).ToList();

			return result;
		}


		public async Task<AssignmentDto> GetAssignmentDetailAsync(string assignmentId)
		{
			var assignment = await _context.LabAssignments.FindAsync(assignmentId);
			if (assignment == null) return null;

			var totalSub = await _context.Submissions.CountAsync(s => s.AssignmentId == assignmentId);
			var passed = await _context.Submissions.CountAsync(s => s.AssignmentId == assignmentId && s.Status == "Passed");

			return new AssignmentDto
			{
				Id = assignment.Id,
				Title = assignment.Title,
				Description = assignment.Description,
				LocTarget = (int)assignment.LocTotal,
				Status = "Open",
				TotalSubmissions = totalSub,
				PassedCount = passed
			};
		}

		private async Task<string> GenerateNewAssignmentIdAsync()
		{
			var lastAssignment = await _context.LabAssignments
				.Where(a => a.Id.StartsWith("ASS-"))
				.OrderByDescending(a => a.Id)
				.FirstOrDefaultAsync();

			int nextNumber = 1;

			if (lastAssignment != null)
			{
				var parts = lastAssignment.Id.Split('-');
				if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
				{
					nextNumber = lastNumber + 1;
				}
			}

			return $"ASS-{nextNumber.ToString("D3")}"; // ví dụ: ASS-001, ASS-025
		}

		public async Task<string> CreateAssignmentAsync(string classId, string teacherId, CreateAssignmentRequest request)
		{
			var newId = await GenerateNewAssignmentIdAsync();
			var assignment = new LabAssignment
			{
				Id = newId,
				Title = request.Title,
				Description = request.Description,
				LocTotal = request.LocTarget,
				TeacherId = teacherId,
				CreatedBy = teacherId,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			var mapping = new ClassHasLabAssignment
			{
				Id = Guid.NewGuid().ToString(),
				AssignmentId = newId,
				ClassId = classId
			};

			await _context.LabAssignments.AddAsync(assignment);
			await _context.ClassHasLabAssignments.AddAsync(mapping);
			await _context.SaveChangesAsync();

			return newId;
		}

		public async Task<bool> UpdateAssignmentAsync(UpdateAssignmentRequest request)
		{
			var assignment = await _context.LabAssignments.FindAsync(request.AssignmentId);
			if (assignment == null) return false;

			assignment.Title = request.Title;
			assignment.Description = request.Description;
			assignment.LocTotal = request.LocTarget;
			assignment.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return true;
		}
	}
}
