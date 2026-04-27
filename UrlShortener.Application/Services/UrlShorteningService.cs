using System;
using System.Collections.Generic;
using System.Text;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services
{

    public record ShortenUrlRequest(string OriginalUrl);
    public record ShortenUrlResponse(string ShortCode);
    public class UrlShorteningService
    {
        private readonly IUrlRepository _repository;
        private readonly IKeyBuffer _keyBuffer;
        private readonly ICacheService _cacheService;

        public UrlShorteningService(IUrlRepository repository, IKeyBuffer keyBuffer, ICacheService cacheService)
        {
            _repository = repository;
            _keyBuffer = keyBuffer;
            _cacheService = cacheService;
        }
        
        public async Task<string> ShortenAsync(string originalUrl, CancellationToken cancellationToken = default)
        {
            var shortCode = await _keyBuffer.GetNextKeyAsync(cancellationToken);
            var urlRecord = UrlRecord.Create(originalUrl, shortCode);

            await _repository.AddAsync(urlRecord, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return shortCode;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            var cachedUrl = await _cacheService.GetOriginalUrlAsync(shortCode, cancellationToken);
            if (!string.IsNullOrEmpty(cachedUrl)) return cachedUrl;

            var record = await _repository.GetByShortCodeAsync(shortCode, cancellationToken);

            if (record != null)
            {
                await _cacheService.SetUrlAsync(shortCode, record.OriginalUrl, cancellationToken);
                return record.OriginalUrl;
            }

            return null;
        }
    }
}
