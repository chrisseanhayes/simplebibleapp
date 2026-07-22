using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using simplebibleapp.LinguisticEngine.Models;
using simplebibleapp.xmldatacore;

using Microsoft.Extensions.Configuration;

namespace simplebibleapp.LinguisticEngine.Cache
{
    /// <summary>
    /// Persistent SQLite cache for <see cref="AgyLinguisticPayloadDto"/> results.
    /// Each row is keyed on (strongs, reference) and stored indefinitely — the data is
    /// lexically static so there is no reason to expire it.
    ///
    /// Table is auto-created the first time <see cref="EnsureTableCreated"/> is called.
    /// </summary>
    public class AgyLinguisticCache
    {
        private readonly string _dbPath;
        private readonly ILogger<AgyLinguisticCache> _logger;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AgyLinguisticCache(IConfiguration configuration, IXmlPathResolver pathResolver, ILogger<AgyLinguisticCache> logger)
        {
            var dataDir = configuration["DataplanePath"] ?? pathResolver.GetPath();
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            
            _dbPath = Path.Combine(dataDir, "agy_cache.db");
            _logger = logger;
        }

        // ── Schema ──────────────────────────────────────────────────────────

        public void EnsureTableCreated()
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS agy_linguistic_cache (
                    strongs     TEXT NOT NULL,
                    reference   TEXT NOT NULL,
                    json_payload TEXT NOT NULL,
                    cached_at   TEXT NOT NULL,
                    PRIMARY KEY (strongs, reference)
                );
                CREATE INDEX IF NOT EXISTS idx_agy_strongs ON agy_linguistic_cache(strongs);
            ";
            cmd.ExecuteNonQuery();
            _logger.LogInformation("AgyLinguisticCache table ensured at {DbPath}", _dbPath);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Try to load a cached payload. Returns null on cache miss.</summary>
        public async Task<AgyLinguisticPayloadDto?> GetAsync(string strongs, string reference, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT json_payload FROM agy_linguistic_cache WHERE strongs = @s AND reference = @r LIMIT 1";
            cmd.Parameters.AddWithValue("@s", strongs);
            cmd.Parameters.AddWithValue("@r", reference);

            var raw = (string?)await cmd.ExecuteScalarAsync(ct);
            if (raw is null) return null;

            try
            {
                return JsonSerializer.Deserialize<AgyLinguisticPayloadDto>(raw, _json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Corrupt cache entry for {Strongs}/{Reference} — will re-fetch.", strongs, reference);
                return null;
            }
        }

        /// <summary>Persist a payload. Upserts on conflict.</summary>
        public async Task SetAsync(string strongs, string reference, AgyLinguisticPayloadDto payload, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(payload, _json);
            var now = DateTimeOffset.UtcNow.ToString("O");

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO agy_linguistic_cache (strongs, reference, json_payload, cached_at)
                VALUES (@s, @r, @j, @t)
                ON CONFLICT(strongs, reference) DO UPDATE SET
                    json_payload = excluded.json_payload,
                    cached_at    = excluded.cached_at;
            ";
            cmd.Parameters.AddWithValue("@s", strongs);
            cmd.Parameters.AddWithValue("@r", reference);
            cmd.Parameters.AddWithValue("@j", json);
            cmd.Parameters.AddWithValue("@t", now);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync(ct);
            return conn;
        }
    }
}
