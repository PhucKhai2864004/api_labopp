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
			await _context.SaveChangesAsync(); // save trước để có assignment.Id

			var mapping = new ClassHasLabAssignment
			{
				ClassId = classId,
				AssignmentId = assignment.Id
			};

			await _context.ClassHasLabAssignments.AddAsync(mapping);
			await _context.SaveChangesAsync();

			return assignment.Id;
		}

		public async Task<bool> UpdateAssignmentAsync(UpdateAssignmentRequest request)
		{
			var assignment = await _context.LabAssignments.FindAsync(request.AssignmentId);
			if (assignment == null) return false;

			assignment.Title = request.Title;
			assignment.Description = request.Description;
			assignment.LocTotal = request.LocTarget;
			assignment.UpdatedAt = DateTime.UtcNow;
			// assignment.UpdatedBy = ??? (truyền teacherId từ context nếu cần)

			await _context.SaveChangesAsync();
			return true;
		}
	}
}
