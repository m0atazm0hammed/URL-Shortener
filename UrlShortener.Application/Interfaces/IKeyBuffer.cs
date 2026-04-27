using System;
using System.Collections.Generic;
using System.Text;

namespace UrlShortener.Application.Interfaces
{
    public interface IKeyBuffer
    {
        ValueTask<string> GetNextKeyAsync(CancellationToken cancellationToken = default);
    }
}
