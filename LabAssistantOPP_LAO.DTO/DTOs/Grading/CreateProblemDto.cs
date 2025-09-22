namespace LabAssistantOPP_LAO.DTO.DTOs.Grading
{
	public class CreateProblemDto
	{
		public int AssignmentId { get; set; }
		public string Title { get; set; }
		public List<TestCaseDto> TestCases { get; set; }
	}

	public class TestCaseDto
	{
		public string Input { get; set; }
		public string ExpectedOutput { get; set; }
		public int? Loc { get; set; }
	}

	public class UpdateTestCaseDto
	{
		public string Input { get; set; }
		public string ExpectedOutput { get; set; }
		public string? Description { get; set; }
		public int? Loc { get; set; }
	}

}
