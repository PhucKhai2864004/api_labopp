using Business_Logic.Interfaces.Admin;
using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Admin
{

    [Route("api/admin/accounts")]
    [ApiController]
    [Authorize(Roles = "Admin,Head Subject")]
    public class AdminAccountController : ControllerBase
    {
        private readonly IUserManagementService _service;
		private readonly LabOopChangeV6Context _context;

		public AdminAccountController(IUserManagementService service, LabOopChangeV6Context context)
        {
            _service = service;
			_context = context;
		}

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var data = await _service.GetUserByIdAsync(id);
            if (data == null)
                return NotFound(ApiResponse<string>.ErrorResponse("User not found"));

            return Ok(ApiResponse<UserDto>.SuccessResponse(data, "OK"));
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] int? roleId, [FromQuery] bool? isActive)
        {
            var data = await _service.GetUsersAsync(keyword, roleId, isActive);
            return Ok(ApiResponse<List<UserDto>>.SuccessResponse(data, "Success"));
        }

		[HttpGet("statistics")]
		public async Task<IActionResult> GetUserStatistics()
		{
			var totalAccounts = await _context.Users.CountAsync(u => u.IsActive);

			var totalStudents = await _context.Users
				.Where(u => u.IsActive && u.Student != null)
			.CountAsync();

			var totalTeachers = await _context.Users
				.Where(u => u.IsActive && u.Teacher != null)
				.CountAsync();

			var result = new
			{
				TotalAccounts = totalAccounts,
				TotalStudents = totalStudents,
				TotalTeachers = totalTeachers
			};

			return Ok(ApiResponse<object>.SuccessResponse(result, "Success"));
		}


		[HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());


            var id = await _service.CreateUserAsync(request);
            return Ok(ApiResponse<int>.SuccessResponse(id, "User created"));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var ok = await _service.UpdateUserAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(ok ? "Updated" : "User not found"));
        }

        [HttpPost("status")]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeUserStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var ok = await _service.ChangeUserStatusAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(ok ? "Status updated" : "User not found"));
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());

            var ok = await _service.ChangePasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(ok ? "Password changed" : "User not found"));
        }

        // 🔁 Reusable helper to extract model state errors
        private ApiResponse<string> ValidationErrorResponse()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return ApiResponse<string>.ErrorResponse("Invalid input", errors);
        }
    }

}
