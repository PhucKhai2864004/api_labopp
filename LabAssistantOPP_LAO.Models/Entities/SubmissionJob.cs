namespace LabAssistantOPP_LAO.Models.Entities;

public class SubmissionJob
{
	public int SubmissionId { get; set; }
	public int ProblemId { get; set; }
	public int TeacherId { get; set; }
	public string MainClass { get; set; }
	public string WorkDir { get; set; }  // đường dẫn thư mục bài nộp đã giải nén
}
