using LabAssistantOPP_LAO.Models.Entities;

namespace NewGradingTest.Models
{
	public class Problem
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public List<TestCase> TestCases { get; set; }
	}
}
