﻿using Business_Logic.Interfaces;
using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Common;
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
		public async Task<IActionResult> GoogleLogin([FromForm] GoogleLoginRequest request)
		{
			var result = await _authService.LoginWithGoogleAsync(request);

			if (!result.Success)
			{
				return Unauthorized(result); // 401 với message cụ thể như "Tài khoản đã bị khóa"
			}

			return Ok(result); // 200 OK với AuthResponse
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] CredentialsLoginRequest request)
		{
			try
			{
				var result = await _authService.LoginWithCredentialsAsync(request);
				return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, "Đăng nhập thành công"));
			}
			catch (Exception ex)
			{
				return Unauthorized(ApiResponse<string>.ErrorResponse("Đăng nhập thất bại", new List<string> { ex.Message }));
			}
		}

	}
}
