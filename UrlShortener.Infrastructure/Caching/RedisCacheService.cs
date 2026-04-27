using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Caching
{
    public class RedisCacheService(IDistributedCache cache) : ICacheService
    {
        private const string CacheKeyPrefix = "url:";

        public async Task<string?> GetOriginalUrlAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return await cache.GetStringAsync($"{CacheKeyPrefix}{shortCode}", cancellationToken);
        }

        public async Task SetUrlAsync(string shortCode, string originalUrl, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };

            await cache.SetStringAsync($"{CacheKeyPrefix}{shortCode}", originalUrl, options, cancellationToken);
        }
    }
}
