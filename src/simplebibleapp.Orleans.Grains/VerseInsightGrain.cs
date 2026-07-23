using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using simplebibleapp.Orleans.Interfaces;

namespace simplebibleapp.Orleans.Grains
{
    [GenerateSerializer]
    public class VerseInsightState
    {
        [Id(0)]
        public string MarkdownBody { get; set; } = string.Empty;
    }

    public class VerseInsightGrain : Grain, IVerseInsightGrain
    {
        private readonly IPersistentState<VerseInsightState> _state;
        private readonly ILogger<VerseInsightGrain> _logger;
        private readonly string _cliExecutablePath;

        public VerseInsightGrain(
            [PersistentState("insight", "blobStorage")] IPersistentState<VerseInsightState> state,
            ILogger<VerseInsightGrain> logger, 
            IConfiguration config)
        {
            _state = state;
            _logger = logger;
            _cliExecutablePath = config["AgyCli:Path"] ?? "agy";
        }

        public Task<string?> GetInsightAsync(string reference)
        {
            if (!string.IsNullOrEmpty(_state.State.MarkdownBody))
            {
                return Task.FromResult<string?>(_state.State.MarkdownBody);
            }
            return Task.FromResult<string?>(null);
        }

        public async Task<string> GetOrGenerateInsightAsync(string reference, string text)
        {
            if (!string.IsNullOrEmpty(_state.State.MarkdownBody))
            {
                return _state.State.MarkdownBody;
            }

            var prompt = $"using kjv, textus receptus, masoretic, septuagint links new to old testament. " +
                         $"Provide insights, context, culture, archaic words, links to old testament, literary nuances for {reference}. " +
                         $"Be brief and find the good stuff.";

            var startInfo = new ProcessStartInfo
            {
                FileName = _cliExecutablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add("gemini-3.6-flash");
            startInfo.ArgumentList.Add("--effort");
            startInfo.ArgumentList.Add("low");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(prompt);

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await process.WaitForExitAsync(cts.Token);

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("AGY CLI failed with code {ExitCode} for verse insight on {Reference}: {Error}", 
                        process.ExitCode, reference, error);
                    throw new InvalidOperationException($"CLI execution failed: {error}");
                }

                _state.State.MarkdownBody = output?.Trim() ?? string.Empty;
                await _state.WriteStateAsync();

                return _state.State.MarkdownBody;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AGY CLI timed out or was cancelled for verse insight on {Reference}", reference);
                if (!process.HasExited) process.Kill();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AGY CLI for reference {Reference}", reference);
                throw;
            }
        }
    }
}
