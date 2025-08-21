using Business_Logic.Services.FapSync;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Head_subject
{
	[Authorize(Roles = "Head Subject")]
	[ApiController]
	[Route("api/fap-sync")]
	public class FAPController : ControllerBase
	{
		private readonly FapSyncService _fapSync;

		public FAPController(FapSyncService fapSync)
		{
			_fapSync = fapSync;
		}

		[HttpPost("sync")]
		public async Task<IActionResult> SyncFap()
		{
			await _fapSync.SyncFapAsync();
			return Ok("FAP sync completed.");
		}
	}
}
