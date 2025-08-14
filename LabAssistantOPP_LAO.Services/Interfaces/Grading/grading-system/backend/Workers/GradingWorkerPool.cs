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
		// Lưu trạng thái pool + classCode của từng teacher
		private readonly Dictionary<string, (bool Running, string ClassCode)> _teacherStatus = new();
		// Workers theo giáo viên
		private readonly Dictionary<string, List<string>> _teacherWorkers = new();
		// Workers + TeacherId để stop
		private readonly Dictionary<string, (WorkerTaskWrapper Worker, string TeacherId)> _namedWorkers = new();


		public GradingWorkerPool(IServiceProvider serviceProvider, ILogger<GradingWorkerPool> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		public bool IsRunning(string teacherId)
		{
			return _teacherStatus.TryGetValue(teacherId, out var status) && status.Running;
		}

		public string? GetClassCode(string teacherId)
		{
			return _teacherStatus.TryGetValue(teacherId, out var status) ? status.ClassCode : null;
		}

		public void Start(int count, string classCode, string teacherId)
		{
			if (IsRunning(teacherId))
			{
				_logger.LogWarning($"⚠️ Teacher {teacherId} already has workers running for class {GetClassCode(teacherId)}. Start request ignored.");
				return;
			}

			_teacherStatus[teacherId] = (true, classCode);
			_logger.LogInformation($"🔄 Starting {count} grading workers for class {classCode} (Teacher {teacherId})...");

			_teacherWorkers[teacherId] = new List<string>();

			for (int i = 1; i <= count; i++)
			{
				var name = $"Worker_{i}_{classCode}_{teacherId}";
				StartWorker(name, teacherId);
			}
		}

		public void StopAllForTeacher(string teacherId)
		{
			if (!_teacherWorkers.ContainsKey(teacherId)) return;

			foreach (var workerName in _teacherWorkers[teacherId])
			{
				if (_namedWorkers.TryGetValue(workerName, out var data))
				{
					data.Worker.CancelToken.Cancel();
					_namedWorkers.Remove(workerName);
					_logger.LogInformation($"⏹ Stopped worker: {workerName}");
				}
			}

			_teacherWorkers.Remove(teacherId);
			if (_teacherStatus.ContainsKey(teacherId))
				_teacherStatus[teacherId] = (false, _teacherStatus[teacherId].ClassCode);
		}

		public bool StartWorker(string name, string teacherId)
		{
			if (_namedWorkers.ContainsKey(name))
				return false;

			var cts = new CancellationTokenSource();
			var task = Task.Run(() => ProcessQueue(name, cts.Token), cts.Token);

			_namedWorkers[name] = (new WorkerTaskWrapper
			{
				Task = task,
				CancelToken = cts
			}, teacherId);

			if (!_teacherWorkers.ContainsKey(teacherId))
				_teacherWorkers[teacherId] = new List<string>();

			_teacherWorkers[teacherId].Add(name);

			_logger.LogInformation($"▶️ Started worker: {name} for teacher {teacherId}");
			return true;
		}

		public bool StopWorker(string name, string teacherId)
		{
			if (!_namedWorkers.TryGetValue(name, out var data))
				return false;

			if (data.TeacherId != teacherId)
				throw new UnauthorizedAccessException("You do not own this worker.");

			data.Worker.CancelToken.Cancel();
			_namedWorkers.Remove(name);
			_teacherWorkers[teacherId]?.Remove(name);
			_logger.LogInformation($"⏹ Stopped worker: {name}");
			return true;
		}

		public List<string> GetActiveWorkerNames(string teacherId)
		{
			if (_teacherWorkers.TryGetValue(teacherId, out var workers))
				return new List<string>(workers);
			return new List<string>();
		}

		public bool IsWorkerOwnedByTeacher(string name, string teacherId)
			=> _namedWorkers.TryGetValue(name, out var data) && data.TeacherId == teacherId;

		[CapSubscribe("submission.created")]
		public void EnqueueJob(SubmissionJob job)
		{
			if (!IsRunning(job.TeacherId)) // 👈 check theo teacher
			{
				_logger.LogWarning($"⚠️ Pool for teacher {job.TeacherId} not running — ignored job {job.SubmissionId}");
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
