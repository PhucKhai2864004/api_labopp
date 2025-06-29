using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
    public class SubmitAssignmentDto
    {
        [Required(ErrorMessage = "AssignmentId không được để trống.")]
        public string AssignmentId { get; set; }

        [Required(ErrorMessage = "ZipFile không được để trống.")]
        public IFormFile ZipFile { get; set; }
    }

}
