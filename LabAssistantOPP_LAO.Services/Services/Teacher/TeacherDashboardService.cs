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
	public class TeacherDashboardService : ITeacherDashboardService
	{
		private readonly LabOopChangeV6Context _context;

		public TeacherDashboardService(LabOopChangeV6Context context)
		{
			_context = context;
		}

		public async Task<TeacherDashboardDto> GetDashboardAsync(int classId)
		{
			var studentIds = await _context.StudentInClasses
				.Where(s => s.ClassId == classId)
				.Select(s => s.StudentId)
				.ToListAsync();

			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(c => c.ClassId == classId)
				.Select(c => c.AssignmentId)
				.ToListAsync();

			var totalAssignments = assignmentIds.Count;
			var totalStudents = studentIds.Count;

			var submissions = await _context.StudentLabAssignments
				.Where(s => assignmentIds.Contains(s.AssignmentId))
				.ToListAsync();

			var waitingReview = submissions.Count(s => s.Status == "Draft");
			var passed = submissions.Count(s => s.Status == "Passed");

			double passRate = totalAssignments * totalStudents == 0 ? 0 :
				Math.Round((double)passed / (totalAssignments * totalStudents) * 100, 2);

			var assignments = await _context.LabAssignments
				.Where(a => assignmentIds.Contains(a.Id))
				.OrderByDescending(a => a.UpdatedAt)
				.Take(5)
				.ToListAsync();

			var recentAssignments = assignments.Select(a => new RecentAssignmentDto
			{
				Title = a.Title,
				Code = a.Id, // giờ là int
				TargetLOC = a.LocTotal ?? 0,
				TotalSubmission = submissions.Count(s => s.AssignmentId == a.Id),
				PassedCount = submissions.Count(s => s.AssignmentId == a.Id && s.Status == "Passed")
			}).ToList();

			var recentSubmissions = submissions
				.OrderByDescending(s => s.SubmittedAt)
				.Take(5)
				.ToList() // in-memory
				.Select(s => new RecentSubmissionDto
				{
					StudentName = _context.Users.FirstOrDefault(u => u.Id == s.StudentId)?.Name ?? "N/A",
					AssignmentCode = s.AssignmentId, // int
					SubmittedAt = s.SubmittedAt ?? DateTime.MinValue,
					Status = s.Status,
					LOC = s.LocResult ?? 0
				}).ToList();

			return new TeacherDashboardDto
			{
				TotalStudents = totalStudents,
				TotalAssignments = totalAssignments,
				SubmissionsWaitingReview = waitingReview,
				PassRate = passRate,
				RecentAssignments = recentAssignments,
				RecentSubmissions = recentSubmissions
			};
		}

		public async Task<List<ClassDto>> GetManagedClassesAsync(int teacherId)
		{
			return await _context.Classes
				.Where(c => c.TeacherId == teacherId)
				.Select(c => new ClassDto
				{
					Id = c.Id,
					Name = c.ClassCode,
					Subject = c.SubjectCode,
					Semester = c.SemesterId,
					AcademicYear = c.AcademicYear,
					LocToPass = c.LocToPass ?? 0,
					IsActive = c.IsActive
				})
				.ToListAsync();
		}
	}
}
