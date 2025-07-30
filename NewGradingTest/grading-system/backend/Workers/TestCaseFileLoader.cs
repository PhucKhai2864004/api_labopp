using LabAssistantOPP_LAO.Models.Entities;
using NewGradingTest.Models;

namespace NewGradingTest.grading_system.backend.Workers
{
	public static class TestCaseFileLoader
	{
		public static List<TestCase> LoadTestCasesFromFolder(string folderPath, string problemId)
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
					Id = $"{problemId}_tc{i + 1}",  // ID định danh duy nhất
					AssignmentId = problemId,
					Input = input,
					ExpectedOutput = output,            // Có thể sửa nếu cần
					CreatedAt = DateTime.UtcNow,
					CreatedBy = "system",
					UpdatedAt = DateTime.UtcNow,
					UpdatedBy = "system"
				};

				testCases.Add(testCase);
			}

			return testCases;
		}
	}
}


