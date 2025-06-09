using Business_Logic.Interfaces;
using Google.Apis.Auth;
using LabAssistantOPP_LAO.DTO.DTOs;
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
		private readonly LabOppContext _context;

		public AuthService(IConfiguration config, LabOppContext context)
		{
			_config = config;
			_context = context;
		}

		public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
		{
			var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
			{
				Audience = new[] { _config["GoogleAuth:ClientId"] }
			});

			var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == payload.Email);

			if (user == null || !user.IsActive)
				throw new UnauthorizedAccessException("User not found or inactive");

			return new AuthResponse
			{
				UserId = user.Id,
				Email = user.Email,
				Role = user.Role?.Name,
				Token = GenerateJwt(user)
			};
		}

		private string GenerateJwt(User user)
		{
			var jwtSettings = _config.GetSection("JwtSettings");
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			if (user.Role == null)
				throw new Exception("User does not have a role assigned.");

			var claims = new[]
			{
		new Claim(JwtRegisteredClaimNames.Sub, user.Id),
		new Claim(JwtRegisteredClaimNames.Email, user.Email),
		new Claim(ClaimTypes.Role, user.Role.Name), // ✅ dùng role từ DB
        new Claim("userId", user.Id)
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
