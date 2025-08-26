using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs.Teacher
{
	public class AssignmentDto
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public int LocTarget { get; set; }
		public DateTime? DueDate { get; set; }
		public string Status { get; set; } // "Open", "Closed"

		public int TotalSubmissions { get; set; }
		public int PassedCount { get; set; }
	}

	public class CreateAssignmentRequest
	{
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title can't exceed 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description can't exceed 1000 characters")]
        public string Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "LOC target must be greater than 0")]
        public int LocTarget { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.DateTime, ErrorMessage = "Invalid date format")]
        public DateTime? DueDate { get; set; }
		public IFormFile? File { get; set; }
	}

	public class UpdateAssignmentRequest : CreateAssignmentRequest
	{
        [Required(ErrorMessage = "Assignment ID is required")]
        public int AssignmentId { get; set; }
	}
}
