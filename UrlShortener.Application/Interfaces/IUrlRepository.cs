using System;
using System.Collections.Generic;
using System.Text;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces
{
    public interface IUrlRepository
    {
        Task<UrlRecord?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
        Task AddAsync(UrlRecord urlRecord, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
