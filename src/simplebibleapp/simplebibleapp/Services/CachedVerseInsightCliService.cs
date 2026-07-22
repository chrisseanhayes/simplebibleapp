using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace simplebibleapp.Services
{
    public class CachedVerseInsightCliService : IVerseInsightCliService
    {
        private readonly IVerseInsightCliService _inner;
        private readonly IMemoryCache _l1;
        private readonly VerseInsightCache _l2;
        private readonly ILogger<CachedVerseInsightCliService> _logger;

        public CachedVerseInsightCliService(
            IVerseInsightCliService inner,
            IMemoryCache l1,
            VerseInsightCache l2,
            ILogger<CachedVerseInsightCliService> logger)
        {
            _inner = inner;
            _l1 = l1;
            _l2 = l2;
            _logger = logger;
        }

        public async Task<string> GetVerseInsightAsync(string verseReference, CancellationToken cancellationToken = default)
        {
            string l1Key = $"verse_insight:{verseReference}";

            // L1: Memory cache
            if (_l1.TryGetValue(l1Key, out string? cached) && !string.IsNullOrEmpty(cached))
            {
                _logger.LogDebug("VerseInsightCache L1 HIT for {Reference}", verseReference);
                return cached;
            }

            // L2: SQLite cache
            var fromDb = await _l2.GetAsync(verseReference, cancellationToken);
            if (!string.IsNullOrEmpty(fromDb))
            {
                _logger.LogDebug("VerseInsightCache L2 HIT for {Reference} — warming L1", verseReference);
                WarmL1(l1Key, fromDb);
                return fromDb;
            }

            // Cache miss: call inner service
            _logger.LogInformation("VerseInsightCache MISS for {Reference} — calling CLI", verseReference);
            var result = await _inner.GetVerseInsightAsync(verseReference, cancellationToken);

            if (!string.IsNullOrEmpty(result))
            {
                WarmL1(l1Key, result);
                await _l2.SetAsync(verseReference, result, cancellationToken);
                _logger.LogInformation("VerseInsightCache stored result for {Reference}", verseReference);
            }

            return result;
        }

        private void WarmL1(string key, string value)
        {
            _l1.Set(key, value, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });
        }
    }
}
