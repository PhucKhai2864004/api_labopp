using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Admin
{
	public interface IRoleService
	{
		Task<List<RoleDto>> GetAllRolesAsync();
	}
}
