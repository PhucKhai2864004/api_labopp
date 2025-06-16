using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
    public class SubmitAssignmentDto
    {
        public string AssignmentId { get; set; }

        public IFormFile ZipFile { get; set; }
    }

}
