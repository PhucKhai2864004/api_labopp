namespace NewGradingTest.Models
{
	public class SubmissionResultDetail
	{
		public string TestCaseId { get; set; }
		public string Status { get; set; } // "PASS" | "FAIL"
		public string ActualOutput { get; set; }
		public string ExpectedOutput { get; set; }
		public int DurationMs { get; set; }
		public string ErrorLog { get; set; }
	}
}
