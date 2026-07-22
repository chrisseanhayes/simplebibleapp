using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using simplebibleapp.xmldatacore;
using Microsoft.Extensions.Configuration;

namespace simplebibleapp.Services
{
    public class VerseInsightCache
    {
        private readonly string _dbPath;
        private readonly ILogger<VerseInsightCache> _logger;

        public VerseInsightCache(IConfiguration configuration, IXmlPathResolver pathResolver, ILogger<VerseInsightCache> logger)
        {
            var dataDir = configuration["DataplanePath"] ?? pathResolver.GetPath();
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            
            _dbPath = Path.Combine(dataDir, "verse_insight_cache.db");
            _logger = logger;
        }

        public void EnsureTableCreated()
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS verse_insight_cache (
                    reference   TEXT NOT NULL,
                    markdown    TEXT NOT NULL,
                    cached_at   TEXT NOT NULL,
                    PRIMARY KEY (reference)
                );
            ";
            cmd.ExecuteNonQuery();
            _logger.LogInformation("VerseInsightCache table ensured at {DbPath}", _dbPath);
        }

        public async Task<string?> GetAsync(string reference, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT markdown FROM verse_insight_cache WHERE reference = @r LIMIT 1";
            cmd.Parameters.AddWithValue("@r", reference);

            var raw = (string?)await cmd.ExecuteScalarAsync(ct);
            return raw;
        }

        public async Task SetAsync(string reference, string markdown, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow.ToString("O");

            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO verse_insight_cache (reference, markdown, cached_at)
                VALUES (@r, @m, @t)
                ON CONFLICT(reference) DO UPDATE SET
                    markdown  = excluded.markdown,
                    cached_at = excluded.cached_at;
            ";
            cmd.Parameters.AddWithValue("@r", reference);
            cmd.Parameters.AddWithValue("@m", markdown);
            cmd.Parameters.AddWithValue("@t", now);

            await cmd.ExecuteNonQueryAsync(ct);
        }

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
