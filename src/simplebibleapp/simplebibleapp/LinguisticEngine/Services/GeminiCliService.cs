using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using simplebibleapp.LinguisticEngine.Models;

namespace simplebibleapp.LinguisticEngine.Services
{
    /// <summary>
    /// Invokes the Antigravity CLI (`agy -p "..."`) in a subprocess, reads the plain-text
    /// JSON response, and deserializes it into <see cref="AgyLinguisticPayloadDto"/>.
    ///
    /// `agy -p` prints the model response directly to stdout with no envelope wrapper,
    /// so deserialization is a single step.
    /// </summary>
    public class AgyCliService : IAgyLinguisticService
    {
        private readonly ILogger<AgyCliService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AgyCliService(ILogger<AgyCliService> logger)
        {
            _logger = logger;
        }

        public async Task<AgyLinguisticPayloadDto?> AnalyzeTokenAsync(
            string anchorStrongs,
            string anchorLemma,
            string language,
            CancellationToken cancellationToken = default)
        {
            string prompt = BuildPrompt(anchorStrongs, anchorLemma, language);

            // Resolve agy binary: prefer PATH lookup, fall back to known install locations
            string agyBin = ResolveBinary("agy");

            var startInfo = new ProcessStartInfo
            {
                FileName = agyBin,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            // agy -p <prompt>  — non-interactive print mode, response goes straight to stdout
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(prompt);

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                // Read stdout and stderr concurrently to prevent deadlocks on large outputs
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cancellationToken);

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError(
                        "agy exited with code {ExitCode} for {Strongs}. Stderr: {Stderr}",
                        process.ExitCode, anchorStrongs, stderr);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    _logger.LogWarning("agy returned empty stdout for {Strongs}", anchorStrongs);
                    return null;
                }

                // agy prints the response text directly — parse it as JSON
                return TryParse(stdout, anchorStrongs);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("agy call was cancelled for {Strongs}", anchorStrongs);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to execute agy synonym analysis for {Strongs}",
                    anchorStrongs);
                throw;
            }
        }

        public Task<AgyLinguisticPayloadDto?> GetCachedTokenAsync(
            string anchorStrongs,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AgyLinguisticPayloadDto?>(null);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the full path to the agy binary, checking known install locations
        /// before falling back to the bare name (relies on PATH at process launch).
        /// </summary>
        private static string ResolveBinary(string name)
        {
            string[] candidates =
            [
                "/root/.local/bin/agy",            // Linux: official install script
                "/usr/local/bin/agy",              // Linux: system-wide
                "/opt/homebrew/bin/agy",           // macOS: Homebrew
                $"/home/{Environment.UserName}/.local/bin/agy", // Linux: user install
            ];

            foreach (var path in candidates)
            {
                if (System.IO.File.Exists(path))
                    return path;
            }

            return name; // Let the OS resolve via PATH
        }

        private AgyLinguisticPayloadDto? TryParse(string raw, string anchorStrongs)
        {
            string cleaned = StripMarkdownFences(raw);
            try
            {
                return JsonSerializer.Deserialize<AgyLinguisticPayloadDto>(cleaned, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "JSON deserialization failed for {Strongs}. Raw output: {Raw}",
                    anchorStrongs, cleaned.Length > 500 ? cleaned[..500] : cleaned);
                return null;
            }
        }

        private static string StripMarkdownFences(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var trimmed = input.Trim();
            if (trimmed.StartsWith("```"))
            {
                var firstNewline = trimmed.IndexOf('\n');
                var lastFence = trimmed.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                    return trimmed.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
            }
            return trimmed;
        }

        private static string BuildPrompt(string anchorStrongs, string anchorLemma, string language)
        {
            const string schema = @"{
  ""target_selection"": {
    ""anchor_strongs"": ""string"",
    ""anchor_lemma"": ""string"",
    ""transliteration"": ""string"",
    ""contextual_gloss"": ""string""
  },
  ""exegetical_context"": {
    ""immediate_context_role"": ""string"",
    ""local_thematic_function"": ""string"",
    ""canonical_trajectory_summary"": ""string""
  },
  ""synonym_network"": [
    {
      ""language"": ""Hebrew | Greek"",
      ""strongs"": ""string"",
      ""lemma"": ""string"",
      ""transliteration"": ""string"",
      ""primary_gloss"": ""string"",
      ""semantic_domain"": ""string"",
      ""relationship"": ""direct_synonym | lxx_translation_equivalent | semantic_neighbor | antonym"",
      ""contextual_confidence"": 0.95,
      ""thematic_alignment_notes"": ""string""
    }
  ]
}";

            return
                "You are a biblical Hebrew and Greek linguistic expert. Analyze the following original-language token in context.\n\n" +
                $"Target Selection:\n" +
                $"- Anchor Strong's: {anchorStrongs}\n" +
                $"- Anchor Lemma: {anchorLemma}\n" +
                $"- Language: {language}\n\n" +
                "Analyze the target token across the biblical canon (MT / TR / LXX).\n" +
                "Return ONLY valid JSON — no prose, no markdown fences — strictly matching this schema:\n\n" +
                schema;
        }
    }
}
