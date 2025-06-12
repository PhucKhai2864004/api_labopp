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
	public class TeacherSubmissionService : ITeacherSubmissionService
	{
		private readonly LabOppContext _context;

		public TeacherSubmissionService(LabOppContext context)
		{
			_context = context;
		}

		public async Task<List<SubmissionDto>> GetSubmissionsWaitingReviewAsync(string classId)
		{
			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var result = await _context.Submissions
				.Where(s => s.Status == "Draft" && assignmentIds.Contains(s.AssignmentId))
				.OrderByDescending(s => s.SubmittedAt)
				.Select(s => new SubmissionDto
				{
					Id = s.Id,
					StudentName = _context.Users.FirstOrDefault(u => u.Id == s.StudentId).Name,
					AssignmentCode = s.AssignmentId,
					SubmittedAt = s.SubmittedAt ?? DateTime.MinValue,
					LOC = s.LocResult ?? 0,
					Status = s.Status,
					FilePath = _context.Files.FirstOrDefault(f => f.Id == s.ZipCode).Path
				}).ToListAsync();

			return result;
		}

		public async Task<SubmissionDto> GetSubmissionDetailAsync(string submissionId)
		{
			var s = await _context.Submissions.FindAsync(submissionId);
			if (s == null) return null;

			var user = await _context.Users.FindAsync(s.StudentId);
			var file = await _context.Files.FindAsync(s.ZipCode);
			var feedback = await _context.Feedbacks.FirstOrDefaultAsync(f => f.SubmissionId == submissionId);

			return new SubmissionDto
			{
				Id = s.Id,
				StudentName = user?.Name ?? "N/A",
				AssignmentCode = s.AssignmentId,
				SubmittedAt = s.SubmittedAt ?? DateTime.MinValue,
				LOC = s.LocResult ?? 0,
				Status = s.Status,
				FilePath = file?.Path,
				Comment = feedback?.Comment ?? ""
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
