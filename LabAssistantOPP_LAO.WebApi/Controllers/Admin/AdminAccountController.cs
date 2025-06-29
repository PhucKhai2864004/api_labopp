using Business_Logic.Interfaces.Admin;
using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Admin
{
    [Route("api/admin/accounts")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAccountController : ControllerBase
    {
        private readonly IUserManagementService _service;

        public AdminAccountController(IUserManagementService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var data = await _service.GetUserByIdAsync(id);
            if (data == null)
                return NotFound(ApiResponse<string>.ErrorResponse("User not found"));

            return Ok(ApiResponse<UserDto>.SuccessResponse(data, "OK"));
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] string? roleId, [FromQuery] bool? isActive)
        {
            var data = await _service.GetUsersAsync(keyword, roleId, isActive);
            return Ok(ApiResponse<List<UserDto>>.SuccessResponse(data, "Success"));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrorResponse());


            var id = await _service.CreateUserAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(id, "User created"));
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
