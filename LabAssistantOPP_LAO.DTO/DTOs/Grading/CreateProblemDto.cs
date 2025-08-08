namespace LabAssistantOPP_LAO.DTO.DTOs.Grading
{
	public class CreateProblemDto
	{
		public string Title { get; set; }
		public List<TestCaseDto> TestCases { get; set; }
	}

	public class TestCaseDto
	{
		public string Input { get; set; }
		public string ExpectedOutput { get; set; }
	}

}
