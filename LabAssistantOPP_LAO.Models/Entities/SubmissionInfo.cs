namespace LabAssistantOPP_LAO.Models.Entities;

public class SubmissionInfo
{
	public int SubmissionId { get; set; }
	public int ProblemId { get; set; }
	public string WorkDir { get; set; }
	public string Status { get; set; } = "Draft"; // mặc định là Draft
	public string MainClass { get; set; } = "Main"; // mặc định
}
