using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Concurrency;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Workers
{
    public class KeyGenerationWorker(IServiceProvider serviceProvider, InMemoryKeyBuffer keyBuffer) : BackgroundService
    {
        private const int MemoryBufferThreshold = 1000;
        private const int DbFetchBatchSize = 2000;
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (keyBuffer.CurrentCount < MemoryBufferThreshold)
                {
                    await ReplenishMemoryBufferAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task ReplenishMemoryBufferAsync(CancellationToken stoppingToken)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var sql = $@"
            DELETE FROM ""UnusedKeys"" 
            WHERE ""Id"" IN (
                SELECT ""Id"" FROM ""UnusedKeys"" 
                LIMIT {DbFetchBatchSize} 
                FOR UPDATE SKIP LOCKED
            ) RETURNING ""ShortCode"";";

            var keysToLoad = await dbContext.Database
                .SqlQueryRaw<string>(sql)
                .ToListAsync(stoppingToken);

            if (!keysToLoad.Any())
            {
                await GenerateAndSaveKeysToDbAsync(dbContext, stoppingToken);
                return;
            }

            foreach (var key in keysToLoad)
            {
                await keyBuffer.AddKeyAsync(key, stoppingToken);
            }
        }

        private async Task GenerateAndSaveKeysToDbAsync(AppDbContext dbContext, CancellationToken stoppingToken)
        {
            var random = new Random();
            var newKeys = new List<UnusedKey>();

            for (int i = 0; i < 10000; i++)
            {
                var shortCode = new string(Enumerable.Repeat(Alphabet, 7)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                newKeys.Add(UnusedKey.Create(shortCode));
            }

            await dbContext.UnusedKeys.AddRangeAsync(newKeys, stoppingToken);

            try
            {
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (DbUpdateException) { }
        }
    }
}
