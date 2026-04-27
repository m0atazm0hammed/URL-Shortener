using System;
using System.Collections.Generic;
using System.Text;

namespace UrlShortener.Domain.Entities
{
    public class UrlRecord
    {
        public Guid Id { get; private set; }
        public string OriginalUrl { get; private set; } = string.Empty;
        public string ShortCode { get; private set; } = string.Empty;
        public DateTime CreatedAtUtc { get; private set; }
        private UrlRecord() { }

        public static UrlRecord Create(string originalUrl, string shortCode)
        {
            if (string.IsNullOrWhiteSpace(originalUrl)) throw new ArgumentException(null, nameof(originalUrl));
            if (string.IsNullOrWhiteSpace(shortCode)) throw new ArgumentException(null, nameof(shortCode));

            return new UrlRecord
            {
                Id = Guid.NewGuid(),
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
