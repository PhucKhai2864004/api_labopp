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

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromForm] RegisterRequest request)
		{
			var result = await _authService.RegisterAsync(request);
			return Ok(result);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromForm] LoginRequest request)
		{
			var result = await _authService.LoginAsync(request);
			return Ok(result);
		}
	}
}
