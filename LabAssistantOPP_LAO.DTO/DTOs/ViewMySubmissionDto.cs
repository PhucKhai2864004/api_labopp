using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
    public class ViewMySubmissionDto
    {
        public int SubmissionId { get; set; }
        public int AssignmentId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? LocResult { get; set; }
        public bool? ManuallyEdited { get; set; }
        public string ManualReason { get; set; }
    }




}
