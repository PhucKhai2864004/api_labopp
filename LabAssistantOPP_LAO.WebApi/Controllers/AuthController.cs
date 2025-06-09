using Business_Logic.Interfaces;
using LabAssistantOPP_LAO.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LabAssistantOPP_LAO.WebApi.Controllers
{
	[ApiController]
	[Route("api/Auth")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;
		public AuthController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpPost("google-login")]
		public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
		{
			try
			{
				var result = await _authService.LoginWithGoogleAsync(request);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return Unauthorized(new { error = ex.Message });
			}
		}
	}
}
