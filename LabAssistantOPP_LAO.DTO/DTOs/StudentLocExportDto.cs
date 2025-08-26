using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
    public class StudentLocExportDto
    {
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public int TotalLoc { get; set; }
        public int Status { get; set; }   // 0 = not passed, 1 = passed
    }
}
