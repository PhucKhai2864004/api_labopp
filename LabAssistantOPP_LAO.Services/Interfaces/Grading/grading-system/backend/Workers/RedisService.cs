using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business_Logic.Interfaces.Grading.grading_system.backend.Workers
{
	public interface IRedisService
	{
		Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
		Task<T?> GetAsync<T>(string key);
		Task RemoveAsync(string key);
		Task<bool> KeyExpireAsync(string key, TimeSpan? expiry);
	}

	public class RedisService : IRedisService
	{
		private readonly IDatabase _db;
		public RedisService(IConnectionMultiplexer redis)
		{
			_db = redis.GetDatabase();
		}

		public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
		{
			var json = JsonSerializer.Serialize(value);
			await _db.StringSetAsync(key, json, expiry);
		}

		public async Task<T?> GetAsync<T>(string key)
		{
			var data = await _db.StringGetAsync(key);
			return data.HasValue ? JsonSerializer.Deserialize<T>(data!) : default;
		}

		public async Task RemoveAsync(string key)
		{
			await _db.KeyDeleteAsync(key);
		}

		public Task<bool> KeyExpireAsync(string key, TimeSpan? expiry)
			=> _db.KeyExpireAsync(key, expiry);
	}

}
