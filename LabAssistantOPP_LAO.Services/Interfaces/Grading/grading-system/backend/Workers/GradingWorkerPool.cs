using Business_Logic.Interfaces.Grading.grading_system.backend.Workers;
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
		private readonly IRedisService _redis;

		// local in-memory queue for jobs (source of truth for job flow)
		private readonly BlockingCollection<SubmissionJob> _jobQueue = new();

		// runtime-only map so we can cancel tasks; persisted state lives in Redis
		private readonly ConcurrentDictionary<string, (WorkerTaskWrapper Worker, int TeacherId)> _namedWorkers = new();

		private static readonly TimeSpan RedisTtl = TimeSpan.FromMinutes(5);
		private const string KeyStatus = "grading:teacher:{0}:status";   // value: { Running: bool, ClassCode: string }
		private const string KeyWorkers = "grading:teacher:{0}:workers"; // value: ["Worker_1_...","..."]

		public GradingWorkerPool(IServiceProvider serviceProvider, ILogger<GradingWorkerPool> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		private IRedisService GetRedis()
		{
			var scope = _serviceProvider.CreateScope();
			return scope.ServiceProvider.GetRequiredService<IRedisService>();
		}

		private record TeacherStatus(bool Running, string ClassCode);

		public async Task<bool> IsRunningAsync(int teacherId)
		{
			var redis = GetRedis();
			var st = await redis.GetAsync<TeacherStatus>(string.Format(KeyStatus, teacherId));
			return st?.Running == true;
		}

		public async Task<string?> GetClassCodeAsync(int teacherId)
		{
			var redis = GetRedis();
			var st = await redis.GetAsync<TeacherStatus>(string.Format(KeyStatus, teacherId));
			return st?.ClassCode;
		}

		public async Task StartAsync(int count, string classCode, int teacherId)
		{
			if (await IsRunningAsync(teacherId))
			{
				_logger.LogWarning("⚠️ Teacher {TeacherId} already has workers running for class {ClassCode}. Start ignored.",
					teacherId, await GetClassCodeAsync(teacherId));
				return;
			}

			var workers = new List<string>();
			for (int i = 1; i <= count; i++)
			{
				var name = $"Worker_{i}_{classCode}_{teacherId}";
				StartWorkerInternal(name, teacherId);
				workers.Add(name);
			}

			var redis = GetRedis();
			await redis.SetAsync(string.Format(KeyStatus, teacherId), new TeacherStatus(true, classCode), RedisTtl);
			await redis.SetAsync(string.Format(KeyWorkers, teacherId), workers, RedisTtl);

			_logger.LogInformation("🔄 Started {Count} grading workers for class {ClassCode} (Teacher {TeacherId})", count, classCode, teacherId);
		}

		public async Task StopAllForTeacherAsync(int teacherId)
		{
			foreach (var kvp in _namedWorkers.Where(x => x.Value.TeacherId == teacherId).ToArray())
			{
				kvp.Value.Worker.CancelToken.Cancel();
				_namedWorkers.TryRemove(kvp.Key, out _);
				_logger.LogInformation("⏹ Stopped worker: {Name}", kvp.Key);
			}

			var redis = GetRedis();
			await redis.RemoveAsync(string.Format(KeyWorkers, teacherId));
			await redis.SetAsync(string.Format(KeyStatus, teacherId),
				new TeacherStatus(false, (await GetClassCodeAsync(teacherId)) ?? string.Empty),
				RedisTtl);
		}

		public async Task<bool> StartWorkerAsync(string name, int teacherId)
		{
			if (_namedWorkers.ContainsKey(name))
				return false;

			StartWorkerInternal(name, teacherId);

			var redis = GetRedis();
			var keyWorkers = string.Format(KeyWorkers, teacherId);
			var list = await redis.GetAsync<List<string>>(keyWorkers) ?? new List<string>();
			if (!list.Contains(name)) list.Add(name);
			await redis.SetAsync(keyWorkers, list, RedisTtl);

			var statusKey = string.Format(KeyStatus, teacherId);
			var st = await redis.GetAsync<TeacherStatus>(statusKey);
			if (st is null || !st.Running)
			{
				var classCode = st?.ClassCode ?? ExtractClassCodeFromWorker(name) ?? "";
				await redis.SetAsync(statusKey, new TeacherStatus(true, classCode), RedisTtl);
			}
			return true;
		}

		public async Task<bool> StopWorkerAsync(string name, int teacherId)
		{
			if (!_namedWorkers.TryGetValue(name, out var data))
				return false;

			if (data.TeacherId != teacherId)
				throw new UnauthorizedAccessException("You do not own this worker.");

			data.Worker.CancelToken.Cancel();
			_namedWorkers.TryRemove(name, out _);

			var redis = GetRedis();
			var keyWorkers = string.Format(KeyWorkers, teacherId);
			var list = await redis.GetAsync<List<string>>(keyWorkers) ?? new List<string>();
			list.Remove(name);
			await redis.SetAsync(keyWorkers, list, RedisTtl);

			return true;
		}

		public Task<List<string>> GetActiveWorkerNamesAsync(int teacherId)
		{
			var redis = GetRedis();
			return redis.GetAsync<List<string>>(string.Format(KeyWorkers, teacherId))
				.ContinueWith(t => t.Result ?? new List<string>());
		}

		private void StartWorkerInternal(string name, int teacherId)
		{
			var cts = new CancellationTokenSource();
			var task = Task.Run(() => ProcessQueue(name, teacherId, cts.Token), cts.Token);

			_namedWorkers[name] = (new WorkerTaskWrapper
			{
				Task = task,
				CancelToken = cts
			}, teacherId);

			_logger.LogInformation("▶️ Started worker: {Name} for teacher {TeacherId}", name, teacherId);
		}

		private static string? ExtractClassCodeFromWorker(string workerName)
		{
			var parts = workerName.Split('_');
			if (parts.Length >= 4)
				return parts[2];
			return null;
		}

		private async Task RefreshTtlAsync(int teacherId)
		{
			var redis = GetRedis();
			await redis.KeyExpireAsync(string.Format(KeyStatus, teacherId), RedisTtl);
			await redis.KeyExpireAsync(string.Format(KeyWorkers, teacherId), RedisTtl);
		}

		[CapSubscribe("submission.created", Group = "grading-worker")]
		public async Task EnqueueJob(SubmissionJob job)
		{
			var running = await IsRunningAsync(job.TeacherId);
			_logger.LogInformation("📥 Received CAP message for Submission {SubmissionId}, PoolRunning={Running}",
				job.SubmissionId, running);

			if (!running)
			{
				_logger.LogWarning("⚠️ Pool for teacher {TeacherId} not running — ignored job {SubmissionId}",
					job.TeacherId, job.SubmissionId);
				return;
			}

			_jobQueue.Add(job);
			_logger.LogInformation("🟢 Job {SubmissionId} enqueued successfully (Teacher {TeacherId})",
				job.SubmissionId, job.TeacherId);
		}

		private async Task ProcessQueue(string workerName, int teacherId, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					if (_jobQueue.TryTake(out var job, millisecondsTimeout: 3000, cancellationToken: token))
					{
						using var scope = _serviceProvider.CreateScope();
						var worker = scope.ServiceProvider.GetRequiredService<SubmissionGradingWorker>();
						_logger.LogInformation("[{Worker}] Processing submission {SubmissionId}", workerName, job.SubmissionId);
						await worker.HandleAsync(job);
					}
					await RefreshTtlAsync(teacherId);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "[{Worker}] Error processing job", workerName);
					await Task.Delay(500, token);
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
