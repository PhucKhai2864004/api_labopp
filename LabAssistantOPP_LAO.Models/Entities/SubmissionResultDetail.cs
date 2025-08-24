namespace LabAssistantOPP_LAO.Models.Entities;

public class SubmissionResultDetail
{
	public int TestCaseId { get; set; }
	public string Status { get; set; } // "PASS" | "FAIL"
	public string ActualOutput { get; set; }
	public string ExpectedOutput { get; set; }
	public string Description { get; set; }
	public int DurationMs { get; set; }
	public string ErrorLog { get; set; }
}
