using Business_Logic.Interfaces.Admin;
using LabAssistantOPP_LAO.DTO.DTOs.Admin;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Logic.Services.Admin
{
	public class RoleService : IRoleService
	{
		private readonly LabOppContext _context;
		public RoleService(LabOppContext context)
		{
			_context = context;
		}

		public async Task<List<RoleDto>> GetAllRolesAsync()
		{
			return await _context.Roles
				.Select(r => new RoleDto
				{
					Id = r.Id,
					Name = r.Name
				})
				.ToListAsync();
		}
	}

}
