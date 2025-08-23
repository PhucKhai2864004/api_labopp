using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Business_Logic.Interfaces.Workers.Docker
{
	public class DockerRunner
	{
		private readonly IConfiguration _configuration;

		public DockerRunner(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task<ExecutionResult> ExecuteAsync(string workDir, string mainClass, string inputPath, string outputPath)
		{
			var startTime = DateTime.UtcNow;

			var ioDir = Path.GetDirectoryName(inputPath)!;
			var dockerIoDir = ioDir.Replace("\\", "/").ToLowerInvariant();

			workDir = Path.GetFullPath(workDir);
			var dockerWorkDir = workDir.Replace("\\", "/").ToLowerInvariant();

			var uid = _configuration["Docker:UID"] ?? "1000";
			var gid = _configuration["Docker:GID"] ?? "1000";

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "docker",
					Arguments = $"run --rm --network none -v \"{dockerWorkDir}:/app\" -v \"{dockerIoDir}:/io\" -w /app my-openjdk17 " +
									$"sh -c \"mkdir -p bin && javac -d bin $(find src -name \"*.java\") && java -cp bin {mainClass} < /io/input.txt > /io/output.txt\"",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};

			process.Start();

			var stderr = await process.StandardError.ReadToEndAsync();
			process.WaitForExit();

			string output = "";
			if (File.Exists(outputPath))
			{
				output = await File.ReadAllTextAsync(outputPath);
			}

			return new ExecutionResult
			{
				Output = output,
				Stderr = stderr,
				DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
			};
		}
	}
	public class ExecutionResult
	{
		public string Output { get; set; }
		public string Stderr { get; set; }
		public int DurationMs { get; set; }
	}
}
