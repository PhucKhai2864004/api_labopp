using LabAssistantOPP_LAO.DTO.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces
{
	public interface IAuthService
	{
		Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
		// Other authentication methods can be added here
	}
}
