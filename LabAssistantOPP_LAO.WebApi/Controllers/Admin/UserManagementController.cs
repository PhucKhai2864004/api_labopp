using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Admin
{
	[Authorize(Roles = "Admin")]
	[ApiController]
	[Route("api/admin")]
	public class UserManagementController : ControllerBase
	{
		[HttpGet]
		public IActionResult GetAllUsers()
		{
			return Ok(new { Message = "Chỉ Admin mới truy cập được API này." });
		}
	}
}
