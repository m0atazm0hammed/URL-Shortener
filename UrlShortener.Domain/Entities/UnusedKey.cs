using System;
using System.Collections.Generic;
using System.Text;

namespace UrlShortener.Domain.Entities
{
    public class UnusedKey
    {
        public long Id { get; private set; }
        public string ShortCode { get; private set; } = string.Empty;

        private UnusedKey() { }

        public static UnusedKey Create(string shortCode)
        {
            return new UnusedKey { ShortCode = shortCode };
        }
    }
}
