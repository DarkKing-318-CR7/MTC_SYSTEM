using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace Helpers
{
    // Path: Caching/PlanCacheSignal.cs
    public static class PlanCacheSignal
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _resetTokens
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        public static IChangeToken GetChangeToken()
        {
            var cts = _resetTokens.GetOrAdd("PlansMasterToken", _ => new CancellationTokenSource());
            return new CancellationChangeToken(cts.Token);
        }

        public static void Reset()
        {
            if (_resetTokens.TryRemove("PlansMasterToken", out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}
