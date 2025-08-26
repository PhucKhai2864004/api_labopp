using LabAssistantOPP_LAO.Models.Entities;

namespace Business_Logic.Interfaces.Workers.Grading
{
	public static class TestCaseFileLoader
	{
		public static List<TestCase> LoadTestCasesFromFolder(string folderPath, int assignmentId, int? systemUserId = null, string? description = null)
		{
			var testCases = new List<TestCase>();

			var inputFiles = Directory.GetFiles(folderPath, "input*.txt").OrderBy(f => f).ToList();
			var outputFiles = Directory.GetFiles(folderPath, "output*.txt").OrderBy(f => f).ToList();

			for (int i = 0; i < inputFiles.Count && i < outputFiles.Count; i++)
			{
				var input = File.ReadAllText(inputFiles[i]);
				var output = File.ReadAllText(outputFiles[i]);

				var testCase = new TestCase
				{
					// Id không cần set vì DB sẽ tự tăng
					AssignmentId = assignmentId,
					Input = input,
					ExpectedOutput = output,
					Description = description ?? $"Test case {i + 1}",
					CreatedAt = DateTime.UtcNow,
					CreatedBy = systemUserId,
					UpdatedAt = DateTime.UtcNow,
					UpdatedBy = systemUserId
				};

				testCases.Add(testCase);
			}

			return testCases;
		}
	}
}


