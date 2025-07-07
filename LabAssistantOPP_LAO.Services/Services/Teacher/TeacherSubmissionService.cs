using Business_Logic.Interfaces.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher;
using LabAssistantOPP_LAO.DTO.DTOs.Teacher.Enum;
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
	public class TeacherSubmissionService : ITeacherSubmissionService
	{
		private readonly LabOppContext _context;

		public TeacherSubmissionService(LabOppContext context)
		{
			_context = context;
		}

		public async Task<List<SubmissionDto>> GetSubmissionsWaitingReviewAsync(string classId, SubmissionStatus? status = null)
		{
			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var query = _context.Submissions
				.Include(x => x.Feedbacks)
				.Where(s => assignmentIds.Contains(s.AssignmentId));

			if (status.HasValue)
			{
				query = query.Where(s => s.Status == status.ToString());
			}

			var submissions = await query
				.OrderByDescending(s => s.SubmittedAt)
				.ToListAsync();

			// Load related data (Users, Files)
			var users = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.Name);
			var files = await _context.Files.ToDictionaryAsync(f => f.Id, f => f.Path);

			return submissions.Select(s => new SubmissionDto
			{
				Id = s.Id,
				StudentName = users.ContainsKey(s.StudentId) ? users[s.StudentId] : "N/A",
				AssignmentCode = s.AssignmentId,
				SubmittedAt = s.SubmittedAt ?? DateTime.MinValue,
				LOC = s.LocResult ?? 0,
				Status = s.Status,
				FilePath = files.ContainsKey(s.ZipCode) ? files[s.ZipCode] : "",
				Comment = s.Feedbacks.Any() ? s.Feedbacks.First().Comment : "No feedback yet"
			}).ToList();
		}


		public async Task<SubmissionDetailDto> GetSubmissionDetailAsync(string submissionId)
		{
			var s = await _context.Submissions
				.Include(x => x.Student)
				.Include(x => x.Assignment)
				.Include(x => x.Feedbacks)
				.Include(x => x.TestCaseResults)
				.Include(x => x.ZipCodeNavigation)
				.FirstOrDefaultAsync(x => x.Id == submissionId);

			if (s == null) return null;

			return new SubmissionDetailDto
			{
				Id = s.Id,
				StudentId = s.StudentId,
				StudentName = s.Student?.Name ?? "N/A",

				AssignmentId = s.AssignmentId,
				AssignmentTitle = s.Assignment?.Title ?? "Unknown",
				LocTarget = s.Assignment?.LocTotal ?? 0,

				SubmittedAt = s.SubmittedAt ?? DateTime.MinValue,
				LOC = s.LocResult ?? 0,
				Status = s.Status ?? "Unknown",

				FilePath = s.ZipCodeNavigation?.Path ?? "",

				Feedbacks = s.Feedbacks.Select(f => f.Comment).ToList(),

				TestCaseResults = s.TestCaseResults.Select(t => new TestCaseResultDto
				{
					Id = t.Id,
					TestCaseId = t.TestCaseId,
					ActualOutput = t.ActualOutput,
					IsPassed = t.IsPassed ?? false
				}).ToList()
			};
		}

		public async Task<bool> GradeSubmissionAsync(string submissionId, bool isPass)
		{
			var submission = await _context.Submissions.FindAsync(submissionId);
			if (submission == null) return false;

			submission.Status = isPass ? "Passed" : "Reject";
			submission.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> SubmitFeedbackAsync(string submissionId, string teacherId, string comment)
		{
			var feedback = await _context.Feedbacks.FirstOrDefaultAsync(f => f.SubmissionId == submissionId);

			if (feedback == null)
			{
				feedback = new Feedback
				{
					Id = Guid.NewGuid().ToString(),
					SubmissionId = submissionId,
					TeacherId = teacherId,
					Comment = comment,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
				await _context.Feedbacks.AddAsync(feedback);
			}
			else
			{
				feedback.Comment = comment;
				feedback.UpdatedAt = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();
			return true;
		}
	}
}
