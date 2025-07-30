namespace NewGradingTest.DTOs
{
	public class SubmitCodeDto
	{
		public string ProblemId { get; set; } = null!; // Lab_Assignment.id

		public string StudentId { get; set; } = null!; // cần để lưu Submission

		public IFormFile ZipFile { get; set; } = null!;

		public string? MainClass { get; set; } // mặc định là "Main"
	}
}
