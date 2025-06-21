using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Admin
{
	public class UserDto
	{
		public string Id { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public string RoleName { get; set; }
		public string Department { get; set; }
		public bool IsActive { get; set; }
		public DateTime? LastActive { get; set; }
	}

	public class CreateUserRequest
	{
		public string FullName { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public string RoleId { get; set; }
		public string Department { get; set; }
	}

	public class UpdateUserRequest : CreateUserRequest
	{
		public string Id { get; set; }
	}

	public class ChangeUserStatusRequest
	{
		public string Id { get; set; }
		public bool IsActive { get; set; }
		public string Reason { get; set; }
	}
}
