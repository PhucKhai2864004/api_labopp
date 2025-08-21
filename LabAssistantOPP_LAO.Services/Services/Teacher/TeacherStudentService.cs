using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services.Teacher
{
	public class TeacherStudentService : ITeacherStudentService
	{
		private readonly LabOopChangeV6Context _context;

		public TeacherStudentService(LabOopChangeV6Context context)
		{
			_context = context;
		}

		public async Task<List<StudentInClassDto>> GetStudentsInClassAsync(int classId)
		{
			var studentIds = await _context.StudentInClasses
				.Where(x => x.ClassId == classId)
				.Select(x => x.StudentId)
				.ToListAsync();

			var assignments = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var submissions = await _context.StudentLabAssignments
				.Where(s => studentIds.Contains(s.StudentId) && assignments.Contains(s.AssignmentId))
				.ToListAsync();

			var users = await _context.Users
				.Where(u => studentIds.Contains(u.Id))
				.ToListAsync();

			var result = users.Select(u => new StudentInClassDto
			{
				StudentId = u.Id,
				FullName = u.Name,
				Email = u.Email,
				TotalAssignments = assignments.Count,
				PassedAssignments = submissions.Count(s => s.StudentId == u.Id && s.Status == "Passed"),
				TotalLOC = submissions
					.Where(s => s.StudentId == u.Id && s.Status == "Passed")
					.Sum(s => s.LocResult ?? 0)
			}).ToList();

			return result;
		}

		public async Task<StudentDetailDto> GetStudentDetailAsync(int classId, int studentId)
		{
			var user = await _context.Users.FindAsync(studentId);
			if (user == null) return null;

			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var assignments = await _context.LabAssignments
				.Where(x => assignmentIds.Contains(x.Id))
				.ToListAsync();

			var submissions = await _context.StudentLabAssignments
				.Where(s => s.StudentId == studentId && assignmentIds.Contains(s.AssignmentId))
				.ToListAsync();

			var progressList = assignments.Select(a =>
			{
				var s = submissions.FirstOrDefault(x => x.AssignmentId == a.Id);
				return new StudentAssignmentProgress
				{
					AssignmentId = a.Id,
					Title = a.Title,
					Status = s?.Status ?? "Not submitted",
					LOC = s?.LocResult ?? 0,
					SubmittedAt = s?.SubmittedAt
				};
			}).ToList();

			return new StudentDetailDto
			{
				StudentId = user.Id,
				FullName = user.Name,
				Email = user.Email,
				Progress = progressList
			};
		}
	}
}
