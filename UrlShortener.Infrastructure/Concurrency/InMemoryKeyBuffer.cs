using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Concurrency
{
    public class InMemoryKeyBuffer : IKeyBuffer
    {
        private readonly Channel<string> _channel;

        public InMemoryKeyBuffer()
        {
            _channel = Channel.CreateUnbounded<string>();
        }

        public async ValueTask<string> GetNextKeyAsync(CancellationToken cancellationToken = default)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }

        public async ValueTask AddKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(key, cancellationToken);
        }

        public int CurrentCount => _channel.Reader.Count;
    }
}
