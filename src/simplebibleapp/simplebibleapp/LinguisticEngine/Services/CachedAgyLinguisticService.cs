using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using simplebibleapp.LinguisticEngine.Cache;
using simplebibleapp.LinguisticEngine.Models;

namespace simplebibleapp.LinguisticEngine.Services
{
    /// <summary>
    /// Two-level cache decorator around <see cref="GeminiCliService"/>:
    ///
    ///   L1 — <see cref="IMemoryCache"/>  (in-process, fast, lost on restart)
    ///   L2 — <see cref="AgyLinguisticCache"/> (SQLite, persistent, survives restarts)
    ///
    /// On a cache miss at both levels the inner service is called and the result is
    /// written back to both layers.  Because the data is lexically static there is
    /// no expiry at the SQLite level; L1 uses a 24-hour sliding window to limit
    /// per-process memory pressure.
    /// </summary>
    public class CachedAgyLinguisticService : IAgyLinguisticService
    {
        private readonly IAgyLinguisticService _inner;
        private readonly IMemoryCache _l1;
        private readonly AgyLinguisticCache _l2;
        private readonly ILogger<CachedAgyLinguisticService> _logger;

        public CachedAgyLinguisticService(
            IAgyLinguisticService inner,
            IMemoryCache l1,
            AgyLinguisticCache l2,
            ILogger<CachedAgyLinguisticService> logger)
        {
            _inner  = inner;
            _l1     = l1;
            _l2     = l2;
            _logger = logger;
        }

        public async Task<AgyLinguisticPayloadDto?> AnalyzeTokenAsync(
            string reference,
            string anchorStrongs,
            string anchorLemma,
            string language,
            CancellationToken cancellationToken = default)
        {
            string l1Key = $"agy:{anchorStrongs}:{reference.Replace(" ", "_")}";

            // ── L1: Memory cache ─────────────────────────────────────────────
            if (_l1.TryGetValue(l1Key, out AgyLinguisticPayloadDto? cached) && cached is not null)
            {
                _logger.LogDebug("AgyCache L1 HIT for {Strongs}/{Reference}", anchorStrongs, reference);
                return cached;
            }

            // ── L2: SQLite cache ─────────────────────────────────────────────
            var fromDb = await _l2.GetAsync(anchorStrongs, reference, cancellationToken);
            if (fromDb is not null)
            {
                _logger.LogDebug("AgyCache L2 HIT for {Strongs}/{Reference} — warming L1", anchorStrongs, reference);
                WarmL1(l1Key, fromDb);
                return fromDb;
            }

            // ── Cache miss: call Gemini CLI ──────────────────────────────────
            _logger.LogInformation("AgyCache MISS for {Strongs}/{Reference} — calling Gemini CLI", anchorStrongs, reference);
            var result = await _inner.AnalyzeTokenAsync(reference, anchorStrongs, anchorLemma, language, cancellationToken);

            if (result is not null)
            {
                // Write-through to both layers
                WarmL1(l1Key, result);
                await _l2.SetAsync(anchorStrongs, reference, result, cancellationToken);
                _logger.LogInformation("AgyCache stored result for {Strongs}/{Reference}", anchorStrongs, reference);
            }

            return result;
        }

        private void WarmL1(string key, AgyLinguisticPayloadDto value)
        {
            _l1.Set(key, value, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });
        }
    }
}
