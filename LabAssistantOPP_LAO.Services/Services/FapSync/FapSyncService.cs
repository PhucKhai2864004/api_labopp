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
			// 1️⃣ Sync Semester
			foreach (var fapSem in _context.FapSemesters.Include(s => s.FapClasses))
			{
				var sem = await _context.Semesters.FirstOrDefaultAsync(s => s.Name == fapSem.Name);
				if (sem == null)
				{
					sem = new Semester { Name = fapSem.Name };
					_context.Semesters.Add(sem);
					await _context.SaveChangesAsync();
				}

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
							TeacherId = 1, // default teacher/admin
							IsActive = true,
							CreatedAt = DateTime.UtcNow
						};
						_context.Classes.Add(cls);
						await _context.SaveChangesAsync();
					}

					// 3️⃣ Sync Student
					foreach (var fapStu in fapCls.FapStudents)
					{
						var existingUser = await _context.Users
							.FirstOrDefaultAsync(u => u.Email == fapStu.StudentCode + "@fap.edu.vn");

						if (existingUser == null)
						{
							var studentRoleId = await _context.Roles
								.Where(r => r.Name == "Student")
								.Select(r => r.Id)
								.FirstOrDefaultAsync();

							var user = new User
							{
								Name = fapStu.Name,
								Email = fapStu.StudentCode + "@fap.edu.vn",
								RoleId = studentRoleId,
								IsActive = true
							};
							_context.Users.Add(user);
							await _context.SaveChangesAsync();

							_context.Students.Add(new Student
							{
								Id = user.Id,
								StudentCode = fapStu.StudentCode
							});
							await _context.SaveChangesAsync();

							// 4️⃣ Map Student -> Class
							_context.StudentInClasses.Add(new StudentInClass
							{
								StudentId = user.Id,
								ClassId = cls.Id
							});
							await _context.SaveChangesAsync();
						}
					}
				}
			}

		}
	}
}
