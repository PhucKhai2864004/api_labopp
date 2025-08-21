using Business_Logic.Interfaces;
using Google.Apis.Auth;
using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services
{
	public class AuthService : IAuthService
	{
		private readonly IConfiguration _config;
		private readonly LabOopChangeV6Context _context;

		public AuthService(IConfiguration config, LabOopChangeV6Context context)
		{
			_config = config;
			_context = context;
		}

		public async Task<ApiResponse<AuthResponse>> LoginWithGoogleAsync(GoogleLoginRequest request)
		{
			var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
			{
				Audience = new[] { _config["GoogleAuth:ClientId"] }
			});

			var user = await _context.Users
				.Include(u => u.Role)
				.FirstOrDefaultAsync(u => u.Email == payload.Email);

			if (user == null)
			{
				return ApiResponse<AuthResponse>.ErrorResponse("Tài khoản không tồn tại trong hệ thống");
			}

			if (!user.IsActive)
			{
				return ApiResponse<AuthResponse>.ErrorResponse("Tài khoản đã bị khóa");
			}

			var token = GenerateJwt(user);

			var response = new AuthResponse
			{
				UserId = user.Id,
				Email = user.Email,
				Role = user.Role?.Name,
				Token = token
			};

			return ApiResponse<AuthResponse>.SuccessResponse(response, "Đăng nhập thành công");
		}

		public async Task<AuthResponse> LoginWithCredentialsAsync(CredentialsLoginRequest request)
		{
			var user = await _context.Users.Include(u => u.Role)
		.FirstOrDefaultAsync(u => u.UserName == request.UserName);

			if (user == null || !user.IsActive)
				throw new UnauthorizedAccessException("User not found or inactive");

			if (user.Password != request.Password)
				throw new UnauthorizedAccessException("Incorrect password");

			var response = new AuthResponse
			{
				UserId = user.Id,
				Email = user.Email,
				Role = user.Role?.Name,
				Token = GenerateJwt(user)
			};

			// Nếu là student -> kiểm tra lớp đang start
			if (user.Role?.Name == "Student")
			{
				var studentClassIds = await _context.StudentInClasses
					.Where(sic => sic.StudentId == user.Id)
					.Select(sic => sic.ClassId)
					.ToListAsync();

				var now = DateTime.UtcNow;
				var activeClass = await _context.ClassSlots
					.Where(cs => studentClassIds.Contains(cs.ClassId)
								 && cs.IsEnabled
								 && cs.StartTime <= now
								 && cs.EndTime >= now)
					.FirstOrDefaultAsync();

				if (activeClass != null)
				{
					response.IsClassActive = true;
					response.ActiveClassId = activeClass.ClassId;
				}
			}

			return response;
		}

//		// Nếu là student -> kiểm tra lớp đang start
//			if (user.Role?.Name == "Student")
//			{
//				var studentClassIds = await _context.StudentInClasses
//					.Where(sic => sic.StudentId == user.Id)
//					.Select(sic => sic.ClassId)
//					.ToListAsync();

//		var now = DateTime.UtcNow;

//		// Tìm slot đang active
//		var activeClass = await _context.ClassSlots
//			.Where(cs => studentClassIds.Contains(cs.ClassId)
//						 && cs.IsEnabled
//						 && cs.StartTime <= now
//						 && cs.EndTime >= now)
//			.FirstOrDefaultAsync();

//				if (activeClass != null)
//				{
//					response.IsClassActive = true;
//					response.ActiveClassId = activeClass.ClassId;
//				}
//				else
//				{
//					// 🔒 Nếu có slot nhưng chưa tới giờ thì chặn login
//					var upcomingClass = await _context.ClassSlots
//						.Where(cs => studentClassIds.Contains(cs.ClassId)
//									 && cs.IsEnabled
//									 && cs.StartTime > now)
//						.OrderBy(cs => cs.StartTime)
//						.FirstOrDefaultAsync();

//					if (upcomingClass != null)
//					{
//						throw new UnauthorizedAccessException("Chưa đến giờ học, không thể đăng nhập");
//}
//				}
//			}


		private string GenerateJwt(User user)
		{
			var jwtSettings = _config.GetSection("JwtSettings");
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			if (user.Role == null)
				throw new Exception("User does not have a role assigned.");

			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),   // 🔑 ép int -> string
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role.Name),
				new Claim("userId", user.Id.ToString()),
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
			};

			var token = new JwtSecurityToken(
				issuer: jwtSettings["Issuer"],
				audience: jwtSettings["Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpireMinutes"])),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
