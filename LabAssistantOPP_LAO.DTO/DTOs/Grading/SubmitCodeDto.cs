using Microsoft.AspNetCore.Http;

namespace LabAssistantOPP_LAO.DTO.DTOs.Grading
{
	public class SubmitCodeDto
	{
		public string ProblemId { get; set; } = null!; // Lab_Assignment.id

		public string StudentId { get; set; } = null!; // cần để lưu Submission

		public IFormFile ZipFile { get; set; } = null!;

		public string Status { get; set; } = "Draft";
	}
}
