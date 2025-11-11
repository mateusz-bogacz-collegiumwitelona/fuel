using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Services.Helpers
{
    public class CacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<CacheService> _logger;
        private readonly IDatabase _db;

        public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
        }

        public static class CacheExpiry
        {
            public static TimeSpan Short => TimeSpan.FromMinutes(5);
            public static TimeSpan Medium => TimeSpan.FromMinutes(30);
            public static TimeSpan Long => TimeSpan.FromHours(2);
            public static TimeSpan VeryLong => TimeSpan.FromHours(24);
        }

        public static class CacheKeys
        {
            public const string BrandPrefix = "brand:";
            public const string AllBrands = "brands:all";
            public const string BrandList = "brands:list";

            public const string FuelTypePrefix = "fueltype:";
            public const string AllFuelTypeCodes = "fueltypes:codes";
            public const string FuelTypeList = "fueltypes:list";

            public const string StationPrefix = "station:";
            public const string StationList = "stations:list";
            public const string StationMap = "stations:map";
            public const string NearestStations = "stations:nearest";

            public const string UserStatsPrefix = "userstats:";
            public const string TopUsers = "users:top";
            public const string UsersList = "users:list";
            public const string UserInfoPrefix = "userinfo:";
        }

        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiry = null) where T : class
        {
            try
            {
                var cachedValue = await _db.StringGetAsync(key);

                if (!cachedValue.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache HIT for key: {Key}", key);
                    return JsonSerializer.Deserialize<T>(cachedValue!);
                }

                _logger.LogDebug("Cache MISS for key: {Key}", key);

                var value = await factory();

                if (value != null)
                {
                    var serialized = JsonSerializer.Serialize(value);
                    await _db.StringSetAsync(
                        key,
                        serialized,
                        expiry ?? CacheExpiry.Medium
                    );
                    _logger.LogDebug("Cached value for key: {Key}", key);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache error for key: {Key}", key);
                return await factory();
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                var result = await _db.KeyDeleteAsync(key);
                _logger.LogDebug("Removed cache key: {Key}", key);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
                return false;
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                var server = _redis.GetServer(endpoints.First());

                var keys = server.Keys(pattern: pattern).ToArray();

                if (keys.Length > 0)
                {
                    await _db.KeyDeleteAsync(keys);
                    _logger.LogInformation("Removed {Count} cache keys matching pattern: {Pattern}",
                        keys.Length, pattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
            }
        }
  
        public async Task InvalidateBrandCacheAsync()
        {
            await RemoveAsync(CacheKeys.AllBrands);
            await RemoveByPatternAsync($"{CacheKeys.BrandPrefix}*");
            await RemoveByPatternAsync($"{CacheKeys.BrandList}*");
            _logger.LogInformation("Invalidated brand cache");
        }

        public async Task InvalidateFuelTypeCacheAsync()
        {
            await RemoveAsync(CacheKeys.AllFuelTypeCodes);
            await RemoveByPatternAsync($"{CacheKeys.FuelTypePrefix}*");
            await RemoveByPatternAsync($"{CacheKeys.FuelTypeList}*");
            _logger.LogInformation("Invalidated fuel type cache");
        }

        public async Task InvalidateStationCacheAsync()
        {
            await RemoveByPatternAsync($"{CacheKeys.StationPrefix}*");
            await RemoveByPatternAsync($"{CacheKeys.StationList}*");
            await RemoveByPatternAsync($"{CacheKeys.StationMap}*");
            await RemoveByPatternAsync($"{CacheKeys.NearestStations}*");
            _logger.LogInformation("Invalidated station cache");
        }

        public async Task InvalidateUserStatsCacheAsync(string email)
        {
            await RemoveAsync($"{CacheKeys.UserStatsPrefix}{email}");
            await RemoveByPatternAsync($"{CacheKeys.TopUsers}*");
            _logger.LogInformation("Invalidated user stats cache for: {Email}", email);
        }

        public async Task InvalidateUserInfoCacheAsync(string? email = null)
        {
            if (!string.IsNullOrEmpty(email))
            {
                await RemoveAsync($"{CacheKeys.UserInfoPrefix}{email}");
                await RemoveAsync($"{CacheKeys.UserStatsPrefix}{email}");
            }
            else
            {
                await RemoveByPatternAsync($"{CacheKeys.UserInfoPrefix}*");
                await RemoveByPatternAsync($"{CacheKeys.UserStatsPrefix}*");
            }

            await RemoveByPatternAsync($"{CacheKeys.TopUsers}*");
            await RemoveByPatternAsync($"{CacheKeys.UsersList}*");
        }

        public string GeneratePagedKey(string baseKey, int pageNumber, int pageSize, string? search = null, string? sortBy = null, string? sortDirection = null)
        {
            var parts = new List<string> { baseKey, pageNumber.ToString(), pageSize.ToString() };

            if (!string.IsNullOrEmpty(search))
                parts.Add($"search:{search.ToLower()}");

            if (!string.IsNullOrEmpty(sortBy))
                parts.Add($"sort:{sortBy.ToLower()}");

            if (!string.IsNullOrEmpty(sortDirection))
                parts.Add($"dir:{sortDirection.ToLower()}");

            return string.Join(":", parts);
        }

        public async Task<List<T>> GetOrSetListAsync<T>(
            string baseKey,
            Func<Task<List<T>>> factory,
            TimeSpan? expiry = null)
        {
            try
            {
                var cachedValue = await _db.StringGetAsync(baseKey);

                if (!cachedValue.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache HIT for list key: {Key}", baseKey);
                    return JsonSerializer.Deserialize<List<T>>(cachedValue!) ?? new List<T>();
                }

                _logger.LogDebug("Cache MISS for list key: {Key}", baseKey);

                var value = await factory();

                if (value != null && value.Any())
                {
                    var serialized = JsonSerializer.Serialize(value);
                    await _db.StringSetAsync(
                        baseKey,
                        serialized,
                        expiry ?? CacheExpiry.Medium
                    );
                    _logger.LogDebug("Cached list for key: {Key} ({Count} items)", baseKey, value.Count);
                }

                return value ?? new List<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache error for list key: {Key}", baseKey);
                return await factory();
            }
        }
    }
}