using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services
{
	public class AuthService
	{
		private readonly LabOppContext _context;
		private readonly IConfiguration _config;

		public AuthService(LabOppContext context, IConfiguration config)
		{
			_context = context;
			_config = config;
		}

		public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
		{
			if (_context.Users.Any(u => u.Email == request.Email))
				throw new Exception("Email already exists");

			var user = new User
			{
				Id = Guid.NewGuid().ToString(),
				Name = request.Name,
				Email = request.Email,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
				RoleId = request.RoleId,
				IsActive = true
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			return new AuthResponse
			{
				UserId = user.Id,
				Email = user.Email,
				Token = GenerateJwt(user)
			};
		}

		public async Task<AuthResponse> LoginAsync(LoginRequest request)
		{
			var user = await _context.Users
				.Include(u => u.Role)
				.FirstOrDefaultAsync(u => u.Email == request.Email);

			if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
				throw new Exception("Invalid credentials");

			return new AuthResponse
			{
				UserId = user.Id,
				Email = user.Email,
				Token = GenerateJwt(user)
			};
		}

		private string GenerateJwt(User user)
		{
			// Đơn giản hóa - bạn có thể yêu cầu tôi viết JWT thực tế nếu muốn
			return "dummy-jwt-token";
		}
	}
}
