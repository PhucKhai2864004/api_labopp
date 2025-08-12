namespace LabAssistantOPP_LAO.Models.Entities;

public class SubmissionJob
{
	public string SubmissionId { get; set; }
	public string ProblemId { get; set; }
	public string TeacherId { get; set; }
	public string MainClass { get; set; }
	public string WorkDir { get; set; }  // đường dẫn thư mục bài nộp đã giải nén
}
