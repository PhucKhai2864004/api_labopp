namespace LabAssistantOPP_LAO.Models.Entities;

public class Problem
{
	public int Id { get; set; }
	public string Title { get; set; }
	public List<TestCase> TestCases { get; set; }
}
