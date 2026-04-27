using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence
{
    public class UrlRepository(AppDbContext context) : IUrlRepository
    {
        public async Task<UrlRecord?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return await context.Urls
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShortCode == shortCode, cancellationToken);
        }

        public async Task AddAsync(UrlRecord urlRecord, CancellationToken cancellationToken = default)
        {
            await context.Urls.AddAsync(urlRecord, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
