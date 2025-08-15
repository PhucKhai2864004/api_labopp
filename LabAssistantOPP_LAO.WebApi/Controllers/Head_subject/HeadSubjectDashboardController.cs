using LabAssistantOPP_LAO.DTO.DTOs;
using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LabAssistantOPP_LAO.WebApi.Controllers.Head_subject
{
    [Authorize(Roles = "Head Subject")]
    [ApiController]
    [Route("api/head_subject/dashboard")]
    public class HeadSubjectDashboardController : ControllerBase
    {
        private readonly LabOppContext _context;
        public HeadSubjectDashboardController(LabOppContext context)
        {
            _context = context;
        }


        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var totalTeachers = await _context.Teachers.CountAsync();

            var totalClasses = await _context.Classes.CountAsync();

            var totalStudents = await _context.Students.CountAsync();

            var result = new
            {
                TotalTeachers = totalTeachers,
                TotalClasses = totalClasses,
                TotalStudents = totalStudents
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "Tổng quan hệ thống"));
        }
    }
}
