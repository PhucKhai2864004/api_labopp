using LabAssistantOPP_LAO.Models.Common;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LabAssistantOPP_LAO.WebApi.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly LabOopChangeV6Context _context;

        public AdminDashboardController(LabOopChangeV6Context context)
        {
            _context = context;
        }

        [HttpGet("recent-timeline-paged")]
        public async Task<IActionResult> GetRecentActivitiesTimelinePaged(int pageNumber = 1, int pageSize = 7)
        {
            try
            {
                // Approval actions
                var approvals = from a in _context.AssignmentApprovals
                                join u in _context.Users on a.ActorId equals u.Id
                                join lab in _context.LabAssignments on a.AssignmentId equals lab.Id
                                select new
                                {
                                    Type = "Approval",
                                    ActorName = u.Name,
                                    Action = a.Action,
                                    Target = lab.Title,
                                    Timestamp = (DateTime?)a.ActedAt
                                };

                // Tương tự cho các query khác
                var slotLogs = from l in _context.ClassSlotLogs
                               join u in _context.Users on l.ActorId equals u.Id
                               join slot in _context.ClassSlots on l.ClassSlotId equals slot.Id
                               select new
                               {
                                   Type = "ClassSlot",
                                   ActorName = u.Name,
                                   Action = l.Action,
                                   Target = $"ClassSlot {slot.SlotNo}",
                                   Timestamp = (DateTime?)l.ActedAt
                               };

                var submissions = from s in _context.StudentLabAssignments
                                  join student in _context.Users on s.StudentId equals student.Id
                                  join lab in _context.LabAssignments on s.AssignmentId equals lab.Id
                                  where s.SubmittedAt != null
                                  select new
                                  {
                                      Type = "Submission",
                                      ActorName = student.Name,
                                      Action = "Submitted",
                                      Target = lab.Title,
                                      Timestamp = s.SubmittedAt
                                  };

                var labAssignments = from l in _context.LabAssignments
                                     join u in _context.Users on l.CreatedBy equals u.Id
                                     select new
                                     {
                                         Type = "LabAssignment",
                                         ActorName = u.Name,
                                         Action = "Created",
                                         Target = l.Title,
                                         Timestamp = (DateTime?)l.CreatedAt
                                     };

                // Giờ Concat sẽ không lỗi nữa
                var allActivities = approvals
                    .AsEnumerable()
                    .Concat(slotLogs.AsEnumerable())
                    .Concat(submissions.AsEnumerable())
                    .Concat(labAssignments.AsEnumerable());

                // Group by day, order descending
                var groupedQuery = allActivities
    .GroupBy(a => a.Timestamp.Value.Date) // dùng .Value để lấy DateTime từ DateTime?
    .OrderByDescending(g => g.Key)
    .Select(g => new
    {
        Date = g.Key,
        Activities = g.OrderByDescending(a => a.Timestamp)
    })
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(ApiResponse<object>.SuccessResponse(groupedQuery));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Failed to get timeline", new List<string> { ex.Message }));
            }
        }

    }
}
