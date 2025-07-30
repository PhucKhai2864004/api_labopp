namespace NewGradingTest.Models
{
	public class SubmissionInfo
	{
		public string SubmissionId { get; set; }
		public string ProblemId { get; set; }
		public string WorkDir { get; set; }
		public string MainClass { get; set; } = "Main"; // mặc định
	}
}
