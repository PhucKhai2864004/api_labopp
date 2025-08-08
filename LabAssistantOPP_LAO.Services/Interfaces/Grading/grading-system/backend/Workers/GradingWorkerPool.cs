using DotNetCore.CAP;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Business_Logic.Interfaces.Workers.Grading
{
	public class GradingWorkerPool : ICapSubscribe
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<GradingWorkerPool> _logger;
		private readonly BlockingCollection<SubmissionJob> _jobQueue = new();
		private readonly Dictionary<string, WorkerTaskWrapper> _namedWorkers = new();

		private bool _isRunning = false;
		public bool IsRunning => _isRunning;

		public GradingWorkerPool(IServiceProvider serviceProvider, ILogger<GradingWorkerPool> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		public void Start(int count, string classCode)
		{
			if (_isRunning) return;

			_isRunning = true;
			_logger.LogInformation($"🔄 Starting {count} grading workers for class {classCode}...");

			for (int i = 1; i <= count; i++)
			{
				var name = $"Worker_{i}_{classCode}";
				StartWorker(name);
			}
		}


		public void Stop()
		{
			if (!_isRunning) return;

			_logger.LogInformation("⏹ Stopping all grading workers...");
			foreach (var worker in _namedWorkers.Values)
			{
				worker.CancelToken.Cancel(); // ❗ Đây là hành động dừng thực sự
			}

			_namedWorkers.Clear();
			_jobQueue.CompleteAdding();
			_isRunning = false;
		}


		public bool StartWorker(string name)
		{
			if (_namedWorkers.ContainsKey(name))
				return false;

			var cts = new CancellationTokenSource();
			var task = Task.Run(() => ProcessQueue(name, cts.Token), cts.Token);

			_namedWorkers[name] = new WorkerTaskWrapper
			{
				Task = task,
				CancelToken = cts
			};

			_logger.LogInformation($"▶️ Started worker: {name}");
			return true;
		}

		public bool StopWorker(string name)
		{
			if (!_namedWorkers.TryGetValue(name, out var worker))
				return false;

			worker.CancelToken.Cancel();
			_namedWorkers.Remove(name);

			_logger.LogInformation($"⏹ Stopped worker: {name}");
			return true;
		}

		public List<string> GetActiveWorkerNames() => _namedWorkers.Keys.ToList();

		[CapSubscribe("submission.created")]
		public void EnqueueJob(SubmissionJob job)
		{
			if (!_isRunning)
			{
				_logger.LogWarning($"⚠️ Pool not running — ignored job {job.SubmissionId}");
				return;
			}

			_logger.LogInformation($"[Queue] Enqueued job {job.SubmissionId}");
			_jobQueue.Add(job);
		}

		private async Task ProcessQueue(string workerName, CancellationToken token)
		{
			foreach (var job in _jobQueue.GetConsumingEnumerable(token))
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var worker = scope.ServiceProvider.GetRequiredService<SubmissionGradingWorker>();
					_logger.LogInformation($"[{workerName}] Processing submission {job.SubmissionId}");
					await worker.HandleAsync(job);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"[{workerName}] Error processing job");
				}
			}
		}

		private class WorkerTaskWrapper
		{
			public Task Task { get; set; } = null!;
			public CancellationTokenSource CancelToken { get; set; } = null!;
		}


	}
}
