﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
	public class AuthResponse
	{
		public string UserId { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }  // Mặc định là STUDENT nếu không có role cụ thể
		public string Token { get; set; }
	}
}
