using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services.FapSync
{
	public class FapSyncService
	{
		private readonly LabOopChangeV6Context _context;

		public FapSyncService(LabOopChangeV6Context context)
		{
			_context = context;
		}
		public async Task SyncFapAsync()
		{
			// load toàn bộ FAP_Semester kèm theo class, student, slot
			var fapSemesters = await _context.FapSemesters
				.Include(s => s.FapClasses)
					.ThenInclude(c => c.FapStudents)
				.Include(s => s.FapClasses)
					.ThenInclude(c => c.FapClassSlots)
				.ToListAsync();

			foreach (var fapSem in fapSemesters)
			{
				// 1️⃣ Sync Semester
				var sem = await _context.Semesters
					.FirstOrDefaultAsync(s => s.Name == fapSem.Code);

				if (sem == null)
				{
					sem = new Semester();
					_context.Semesters.Add(sem);
				}
				sem.Name = fapSem.Code;
				sem.StartDate = fapSem.StartDate;
				sem.EndDate = fapSem.EndDate;
				await _context.SaveChangesAsync();

				// 2️⃣ Sync Class
				foreach (var fapCls in fapSem.FapClasses)
				{
					var cls = await _context.Classes
						.FirstOrDefaultAsync(c => c.ClassCode == fapCls.Code && c.SemesterId == sem.Id);

					if (cls == null)
					{
						cls = new Class
						{
							ClassCode = fapCls.Code,
							SemesterId = sem.Id,
							SubjectCode = fapCls.SubjectCode,
							AcademicYear = fapCls.AcademicYear,
							LocToPass = 750,
							TeacherId = fapCls.TeacherId, // default teacher/admin
							IsActive = true,
							CreatedAt = DateTime.UtcNow
						};
						_context.Classes.Add(cls);
					}
					cls.ClassCode = fapCls.Code;
					cls.SubjectCode = fapCls.SubjectCode;
					cls.AcademicYear = fapCls.AcademicYear;
					cls.LocToPass = 750;
					cls.TeacherId = fapCls.TeacherId; // default teacher/admin
					cls.IsActive = true;

					await _context.SaveChangesAsync();

					// 3️⃣ Sync Student
					foreach (var fapStu in fapCls.FapStudents)
					{
						var email = fapStu.Username + "@fpt.edu.vn";

						// tìm User
						var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
						if (user == null)
						{
							var studentRoleId = await _context.Roles
								.Where(r => r.Name == "Student")
								.Select(r => r.Id)
								.FirstOrDefaultAsync();

							user = new User
							{
								Email = email,
								UserName = fapStu.Username,
								RoleId = studentRoleId,
								IsActive = true,
								CreatedAt = DateTime.UtcNow
							};
							_context.Users.Add(user);
							await _context.SaveChangesAsync();
						}

						// 🔄 luôn update (ghi đè)
						user.Name = fapStu.Name;
						user.Password = fapStu.Password; // TODO: mã hóa

						// tìm Student
						var student = await _context.Students
							.FirstOrDefaultAsync(s => s.StudentCode == fapStu.StudentCode);

						if (student == null)
						{
							student = new Student { Id = user.Id };
							_context.Students.Add(student);
						}

						// 🔄 luôn update
						student.StudentCode = fapStu.StudentCode;
						student.Major = fapStu.Major;
						student.DateOfBirth = fapStu.DateOfBirth;
						student.Phone = fapStu.Phone;
						student.Gender = fapStu.Gender;
						student.Address = fapStu.Address;

						// mapping vào class
						if (!await _context.StudentInClasses
							.AnyAsync(sc => sc.StudentId == user.Id && sc.ClassId == cls.Id))
						{
							_context.StudentInClasses.Add(new StudentInClass
							{
								StudentId = user.Id,
								ClassId = cls.Id
							});
						}
					}

					// 4️⃣ Sync ClassSlot
					foreach (var fapSlot in fapCls.FapClassSlots)
					{
						var slot = await _context.ClassSlots
							.FirstOrDefaultAsync(s => s.ClassId == cls.Id && s.SlotNo == fapSlot.SlotNo);

						if (slot == null)
						{
							slot = new ClassSlot
							{
								ClassId = cls.Id,
								SlotNo = fapSlot.SlotNo,
								StartTime = fapSlot.StartTime,
								EndTime = fapSlot.EndTime,
								IsEnabled = fapSlot.IsEnabled,
								ServerEndpoint = fapSlot.ServerEndpoint,
								Note = fapSlot.Note
							};
							_context.ClassSlots.Add(slot);
						}
						else
						{
							// update nếu đã có
							slot.StartTime = fapSlot.StartTime;
							slot.EndTime = fapSlot.EndTime;
							slot.IsEnabled = fapSlot.IsEnabled;
							slot.ServerEndpoint = fapSlot.ServerEndpoint;
							slot.Note = fapSlot.Note;
						}
					}
					await _context.SaveChangesAsync();
				}
			}

		}
	}
}
