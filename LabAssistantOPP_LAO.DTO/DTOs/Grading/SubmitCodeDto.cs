using Microsoft.AspNetCore.Http;

namespace LabAssistantOPP_LAO.DTO.DTOs.Grading
{
	public class SubmitCodeDto
	{
		public int ProblemId { get; set; } // Lab_Assignment.id

		public int StudentId { get; set; } // cần để lưu Submission

		public int SemesterId { get; set; } // cần để lưu Submission

		public IFormFile ZipFile { get; set; } = null!;

		public string Status { get; set; } = "Draft";
	}
}
