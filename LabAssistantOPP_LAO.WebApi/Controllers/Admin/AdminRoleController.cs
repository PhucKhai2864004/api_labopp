using Business_Logic.Interfaces.Admin;
using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using LabAssistantOPP_LAO.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Admin
{
	[Route("api/admin/roles")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class AdminRoleController : ControllerBase
	{
		private readonly IRoleService _roleService;

		public AdminRoleController(IRoleService roleService)
		{
			_roleService = roleService;
		}

		[HttpGet]
		public async Task<IActionResult> GetAllRoles()
		{
			var roles = await _roleService.GetAllRolesAsync();
			return Ok(ApiResponse<List<RoleDto>>.SuccessResponse(roles, "OK"));
		}
	}
}
