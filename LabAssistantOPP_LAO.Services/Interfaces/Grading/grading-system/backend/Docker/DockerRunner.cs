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
				// 1. Compile Java
				var compile = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "javac",
						Arguments = $"-encoding UTF-8 -d bin " + string.Join(" ",
								Directory.GetFiles(workDir, "*.java", SearchOption.AllDirectories)
										 .Select(f => "\"" + Path.GetRelativePath(workDir, f).Replace("\\", "/") + "\"")),

						// Linux
						// Nếu Windows thì:
						// Arguments = $"-d bin {string.Join(" ", Directory.GetFiles(Path.Combine(workDir, "src"), "*.java", SearchOption.AllDirectories))}",
						RedirectStandardError = true,
						RedirectStandardOutput = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = workDir
					}
				};

				Directory.CreateDirectory(Path.Combine(workDir, "bin"));
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

				// 2. Run Java program
				var run = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "java",
						Arguments = $"-cp bin {mainClass}",
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = workDir
					}
				};

				run.Start();

				// Ghi input vào stdin
				if (File.Exists(inputPath))
				{
					var input = await File.ReadAllTextAsync(inputPath);
					await run.StandardInput.WriteAsync(input);
					run.StandardInput.Close();
				}

				output = await run.StandardOutput.ReadToEndAsync();
				stderr = await run.StandardError.ReadToEndAsync();

				run.WaitForExit();

				// Lưu output ra file
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
	}
	public class ExecutionResult
	{
		public string Output { get; set; }
		public string Stderr { get; set; }
		public int DurationMs { get; set; }
	}
}
