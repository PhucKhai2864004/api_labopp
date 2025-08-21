using Business_Logic.Interfaces.Admin;
using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.EntityFrameworkCore;
using LabAssistantOPP_LAO.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services.Admin
{
	public class UserManagementService : IUserManagementService
	{
		private readonly LabOopChangeV6Context _context;

		public UserManagementService(LabOopChangeV6Context context)
		{
			_context = context;
		}

		public async Task<List<UserDto>> GetUsersAsync(string? keyword, int? roleId, bool? isActive)
		{
			var query = _context.Users.Include(u => u.Role).AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				query = query.Where(u =>
					u.Name.Contains(keyword) ||
					u.Email.Contains(keyword));
			}

			if (roleId.HasValue)
			{
				query = query.Where(u => u.RoleId == roleId.Value);
			}

			if (isActive.HasValue)
			{
				query = query.Where(u => u.IsActive == isActive.Value);
			}

			return await query
				.OrderBy(u => u.Name)
				.Select(u => new UserDto
				{
					Id = u.Id,
					FullName = u.Name,
					Email = u.Email,
					Phone = "", // Nếu DB có thì map
					RoleName = u.Role != null ? u.Role.Name : "",
					Department = "", // Nếu DB có thì map
					IsActive = u.IsActive,
					LastActive = u.UpdatedAt
				}).ToListAsync();
		}

		
		public async Task<int> CreateUserAsync(CreateUserRequest request)
		{
			var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
			if (role == null)
				throw new Exception("Invalid role");

			var user = new User
			{
				// Id sẽ do DB tự generate (IDENTITY)
				Name = request.FullName,
				Email = request.Email,
				RoleId = request.RoleId,
				IsActive = true,
				UserName = request.UserName,
				Password = request.Password, // TODO: hash sau
				CreatedBy = 1, // admin userId
				CreatedAt = DateTime.UtcNow,
				UpdatedBy = 1,
				UpdatedAt = DateTime.UtcNow
			};

			await _context.Users.AddAsync(user);
			await _context.SaveChangesAsync();
			return user.Id; // trả về int id
		}

		public async Task<bool> UpdateUserAsync(UpdateUserRequest request)
		{
			var user = await _context.Users.FindAsync(request.Id);
			if (user == null) return false;

			user.Name = request.FullName;
			//user.Email = request.Email;
			//user.RoleId = request.RoleId;
			user.UpdatedAt = DateTime.UtcNow;
			user.UpdatedBy = 1; // admin

			if (!string.IsNullOrWhiteSpace(request.Password))
			{
				user.Password = request.Password;  // TODO: hash sau
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ChangeUserStatusAsync(ChangeUserStatusRequest request)
		{
			var user = await _context.Users.FindAsync(request.Id);
			if (user == null) return false;

			user.IsActive = request.IsActive;
			user.UpdatedAt = DateTime.UtcNow;
			user.UpdatedBy = 1; // admin

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<UserDto?> GetUserByIdAsync(int id)
		{
			var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
			if (user == null) return null;

			return new UserDto
			{
				Id = user.Id,
				FullName = user.Name,
				Email = user.Email,
				RoleName = user.Role?.Name ?? "",
				Department = "", // nếu DB có thì map
				Phone = "",      // nếu DB có thì map
				IsActive = user.IsActive,
				LastActive = user.UpdatedAt
			};
		}

		public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
		{
			var user = await _context.Users.FindAsync(request.UserId);
			if (user == null) return false;

			user.Password = request.NewPassword; // TODO: hash sau
			user.UpdatedAt = DateTime.UtcNow;
			user.UpdatedBy = 1; // admin

			await _context.SaveChangesAsync();
			return true;
		}

	}

}
