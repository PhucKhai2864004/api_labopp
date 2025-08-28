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
		private readonly LabOopChangeV6Context _context;

		public TeacherSubmissionService(LabOopChangeV6Context context)
		{
			_context = context;
		}

		public async Task<List<SubmissionDto>> GetSubmissionsWaitingReviewAsync(int classId, SubmissionStatus? status = null)
		{
			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var query = _context.StudentLabAssignments
				.Include(x => x.Student)
				.Where(s => assignmentIds.Contains(s.AssignmentId));

			if (status.HasValue)
			{
				query = query.Where(s => s.Status == status.ToString());
			}

			var submissions = await query
				.OrderByDescending(s => s.SubmittedAt)
				.ToListAsync();

			return submissions.Select(s => new SubmissionDto
			{
				Id = s.Id,
				StudentName = s.Student?.Name ?? "N/A",
				AssignmentCode = s.AssignmentId,
				SubmittedAt = s.SubmittedAt ?? DateTime.MinValue,
				LOC = s.LocResult ?? 0,
				Status = s.Status,
				FilePath = s.SubmissionZip ?? "",
				Comment = s.ManualReason ?? "No feedback yet"
			}).ToList();
		}


		public async Task<SubmissionDetailDto> GetSubmissionDetailAsync(int submissionId)
		{
			var s = await _context.StudentLabAssignments
				.Include(x => x.Student)
				.Include(x => x.Assignment)
				.Include(x => x.TestCaseResults)
				.ThenInclude(t => t.TestCase)
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

				FilePath = s.SubmissionZip ?? "",

				Feedbacks = string.IsNullOrEmpty(s.ManualReason) ? new List<string>() : new List<string> { s.ManualReason },

				TestCaseResults = s.TestCaseResults.Select(t => new TestCaseResultDto
				{
					Id = t.Id,
					TestCaseId = t.TestCaseId,
					ActualOutput = t.ActualOutput,
					IsPassed = t.IsPassed ?? false
				}).ToList()
			};
		}

		public async Task<bool> GradeSubmissionAsync(int submissionId, string status)
		{
			var submission = await _context.StudentLabAssignments
				.Include(s => s.Assignment)
				.FirstOrDefaultAsync(s => s.Id == submissionId);

			if (submission == null)
				return false;

			// ✅ chỉ chấm nếu submission đang là "Submit"
			if (!string.Equals(submission.Status, "Submit", StringComparison.OrdinalIgnoreCase))
				return false;

			submission.Status = status; // "Passed" hoặc "Reject"

			if (string.Equals(status, "Passed", StringComparison.OrdinalIgnoreCase)
				&& submission.Assignment?.LocTotal != null)
			{
				submission.LocResult = (submission.LocResult ?? 0) + submission.Assignment.LocTotal.Value;
			}

			submission.ManuallyEdited = true;
			submission.SubmittedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return true;
		}


		public async Task<bool> SubmitFeedbackAsync(int submissionId, int teacherId, string comment)
		{
			var submission = await _context.StudentLabAssignments.FindAsync(submissionId);
			if (submission == null) return false;

			submission.ManualReason = comment;
			submission.ManuallyEdited = true;

			await _context.SaveChangesAsync();
			return true;
		}
	}
}
