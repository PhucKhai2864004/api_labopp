using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
    public class ClassPassRateDto
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public int TotalStudents { get; set; }
        public int StudentsPassed { get; set; }
        public double PassRate { get; set; } // %
    }



}
