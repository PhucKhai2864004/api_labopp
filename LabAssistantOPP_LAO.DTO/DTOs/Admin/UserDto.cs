using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Admin
{
	public class UserDto
	{
		public int Id { get; set; }
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

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name can't be longer than 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Role ID is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 50 characters")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }
    }

	public class UpdateUserRequest : CreateUserRequest
	{
        [Required(ErrorMessage = "User ID is required")]
        public string Id { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public new string? Password { get; set; }
    }



	public class ChangeUserStatusRequest
	{
        [Required(ErrorMessage = "User ID is required")]
        public string Id { get; set; }
		public bool IsActive { get; set; }
		public string Reason { get; set; }
	}



	public class ChangePasswordRequest
	{
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }
    }


}
