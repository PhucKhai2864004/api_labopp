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
	public class TeacherLocService : ITeacherLocService
	{
		private readonly LabOopChangeV6Context _context;

		public TeacherLocService(LabOopChangeV6Context context)
		{
			_context = context;
		}

		public async Task<List<LocRankingDto>> GetLocRankingAsync(int classId)
		{
			var studentIds = await _context.StudentInClasses
				.Where(x => x.ClassId == classId)
				.Select(x => x.StudentId)
				.ToListAsync();

			var assignmentIds = await _context.ClassHasLabAssignments
				.Where(x => x.ClassId == classId)
				.Select(x => x.AssignmentId)
				.ToListAsync();

			var submissions = await _context.StudentLabAssignments
				.Where(s => studentIds.Contains(s.StudentId)
						 && assignmentIds.Contains(s.AssignmentId)
						 && s.Status == "Passed")
				.ToListAsync();

			var users = await _context.Users
				.Where(u => studentIds.Contains(u.Id))
				.ToListAsync();

			var rankings = users.Select(u => new LocRankingDto
			{
				StudentId = u.Id,
				FullName = u.Name,
				Email = u.Email,
				PassedAssignments = submissions.Count(s => s.StudentId == u.Id),
				TotalLOC = submissions
							.Where(s => s.StudentId == u.Id)
							.Sum(s => s.LocResult ?? 0)
			})
			.OrderByDescending(r => r.TotalLOC)
			.Select((r, index) =>
			{
				r.Rank = index + 1;
				return r;
			})
			.ToList();

			return rankings;
		}
	}
}
