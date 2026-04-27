using System;
using System.Collections.Generic;
using System.Text;

namespace UrlShortener.Application.Interfaces
{
    public interface ICacheService
    {
        Task<string?> GetOriginalUrlAsync(string shortCode, CancellationToken cancellationToken = default);
        Task SetUrlAsync(string shortCode, string originalUrl, CancellationToken cancellationToken = default);
    }
}
