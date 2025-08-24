using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Business_Logic.Interfaces.Workers.Docker
{
	public class DockerRunner
	{
		public async Task<ExecutionResult> ExecuteAsync(string workDir, string mainClass, string inputPath, string outputPath)
		{
			var startTime = DateTime.UtcNow;
			string stderr = "";
			string output = "";


			try
			{
				// Detect source directory
				var sourceDir = DetectSourceDir(workDir);

				// Tìm tất cả file .java
				var sourceFiles = Directory.GetFiles(sourceDir, "*.java", SearchOption.AllDirectories)
										   .Select(f => Path.GetRelativePath(workDir, f).Replace("\\", "/"))
										   .ToList();

				if (!sourceFiles.Any())
				{
					return new ExecutionResult
					{
						Output = "",
						Stderr = "No Java source files found.",
						DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
					};
				}

				// Tạo file chứa danh sách nguồn (javac hỗ trợ @sources.txt)
				var sourcesFile = Path.Combine(workDir, "sources.txt");
				await File.WriteAllLinesAsync(sourcesFile, sourceFiles);

				// Đảm bảo có bin directory
				Directory.CreateDirectory(Path.Combine(workDir, "bin"));

				// 1. Compile
				var compile = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "javac",
						Arguments = $"-encoding UTF-8 -d bin @sources.txt",
						RedirectStandardError = true,
						RedirectStandardOutput = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = workDir
					}
				};

				compile.Start();
				var compileErr = await compile.StandardError.ReadToEndAsync();
				compile.WaitForExit();

				if (compile.ExitCode != 0)
				{
					return new ExecutionResult
					{
						Output = "",
						Stderr = compileErr,
						DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
					};
				}

				// 2. Run
				var run = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "java",
						Arguments = $"-cp bin " + mainClass,
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = workDir
					}
				};

				run.Start();

				// Nếu có input thì ghi vào stdin
				if (File.Exists(inputPath))
				{
					var input = await File.ReadAllTextAsync(inputPath);
					await run.StandardInput.WriteAsync(input);
					run.StandardInput.Close();
				}

				output = await run.StandardOutput.ReadToEndAsync();
				stderr = await run.StandardError.ReadToEndAsync();

				run.WaitForExit();

				// Ghi output ra file
				await File.WriteAllTextAsync(outputPath, output);
			}
			catch (Exception ex)
			{
				stderr = ex.ToString();
			}

			return new ExecutionResult
			{
				Output = output,
				Stderr = stderr,
				DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
			};
		}

		private string DetectSourceDir(string workDir)
		{
			var srcPath = Path.Combine(workDir, "src");
			if (Directory.Exists(srcPath))
				return srcPath;

			return workDir;
		}
	}

	public class ExecutionResult
	{
		public string Output { get; set; }
		public string Stderr { get; set; }
		public int DurationMs { get; set; }
	}
}
