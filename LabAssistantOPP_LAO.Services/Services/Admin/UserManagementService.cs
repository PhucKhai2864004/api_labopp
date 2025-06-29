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
		private readonly LabOppContext _context;

		public UserManagementService(LabOppContext context)
		{
			_context = context;
		}

		public async Task<List<UserDto>> GetUsersAsync(string? keyword, string? roleId, bool? isActive)
		{
			var query = _context.Users.Include(u => u.Role).AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				query = query.Where(u =>
					u.Name.Contains(keyword) ||
					u.Email.Contains(keyword));
			}

			if (!string.IsNullOrEmpty(roleId))
			{
				query = query.Where(u => u.RoleId == roleId);
			}

			if (isActive.HasValue)
			{
				query = query.Where(u => u.IsActive == isActive);
			}

			return await query
				.OrderBy(u => u.Name)
				.Select(u => new UserDto
				{
					Id = u.Id,
					FullName = u.Name,
					Email = u.Email,
					Phone = "", // Add later if schema has
					RoleName = u.Role != null ? u.Role.Name : "",
					Department = "", // Add if stored
					IsActive = (bool)u.IsActive,
					LastActive = u.UpdatedAt
				}).ToListAsync();
		}

		private async Task<string> GenerateUserIdAsync(string roleName)
		{
			string prefix = roleName switch
			{
				"Student" => "HE",
				"Teacher" => "GV",
				"Admin" => "AD",
				_ => "US"
			};

			var lastUser = await _context.Users
				.Where(u => u.Id.StartsWith(prefix))
				.OrderByDescending(u => u.Id)
				.FirstOrDefaultAsync();

			int nextNumber = 1;

			if (lastUser != null && int.TryParse(lastUser.Id.Substring(prefix.Length), out int lastNumber))
			{
				nextNumber = lastNumber + 1;
			}

			return $"{prefix}{nextNumber.ToString("D6")}"; // HE000001, GV000123
		}

		public async Task<string> CreateUserAsync(CreateUserRequest request)
		{
			var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
			if (role == null)
				throw new Exception("Invalid role");

			var newId = await GenerateUserIdAsync(role.Name);

			var user = new User
			{
				Id = newId,
				Name = request.FullName,
				Email = request.Email,
				RoleId = request.RoleId,
				IsActive = true,
				CreatedBy = "admin",
				CreatedAt = DateTime.UtcNow,
				UpdatedBy = "admin",
				UpdatedAt = DateTime.UtcNow
			};

			await _context.Users.AddAsync(user);
			await _context.SaveChangesAsync();
			return newId;
		}

		public async Task<bool> UpdateUserAsync(UpdateUserRequest request)
		{
			var user = await _context.Users.FindAsync(request.Id);
			if (user == null) return false;

			user.Name = request.FullName;
			//user.Email = request.Email;
			//user.RoleId = request.RoleId;
			user.UpdatedAt = DateTime.UtcNow;
			user.UpdatedBy = "admin";

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ChangeUserStatusAsync(ChangeUserStatusRequest request)
		{
			var user = await _context.Users.FindAsync(request.Id);
			if (user == null) return false;

			user.IsActive = request.IsActive;
			user.UpdatedAt = DateTime.UtcNow;
			user.UpdatedBy = "admin";

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<UserDto?> GetUserByIdAsync(string id)
		{
			var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
			if (user == null) return null;

			return new UserDto
			{
				Id = user.Id,
				FullName = user.Name,
				Email = user.Email,
				RoleName = user.Role?.Name ?? "",
				Department = "", // nếu bạn lưu thêm
				Phone = "",      // nếu có
				IsActive = (bool)user.IsActive,
				LastActive = user.UpdatedAt
			};
		}
        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return false;

            user.Password = request.NewPassword; //Plain text hiện tại
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "admin";

            await _context.SaveChangesAsync();
            return true;
        }

    }
}
