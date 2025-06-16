using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Admin
{
	public interface IUserManagementService
	{
		Task<List<UserDto>> GetUsersAsync(string? keyword, string? roleId, bool? isActive);
		Task<string> CreateUserAsync(CreateUserRequest request);
		Task<bool> UpdateUserAsync(UpdateUserRequest request);
		Task<bool> ChangeUserStatusAsync(ChangeUserStatusRequest request);
		Task<UserDto?> GetUserByIdAsync(string id);
	}
}
