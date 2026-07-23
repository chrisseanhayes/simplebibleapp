using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using simplebibleapp.Orleans.Interfaces;

namespace simplebibleapp.Orleans.Grains
{
    public class VerseNoteJobGrain : Grain, IVerseNoteJobGrain
    {
        private readonly ILogger<VerseNoteJobGrain> _logger;
        private readonly string _cliExecutablePath;

        public VerseNoteJobGrain(ILogger<VerseNoteJobGrain> logger, IConfiguration config)
        {
            _logger = logger;
            _cliExecutablePath = config["AgyCli:Path"] ?? "agy";
        }

        public async Task<string> GenerateNoteAsync(string reference, string question)
        {
            // Build a focused prompt that gives the AI the verse context + the user's question
            var prompt =
                $"Bible verse context: {reference} (KJV / Textus Receptus / Masoretic). " +
                $"User question: \"{question}\". " +
                $"Answer the question directly and concisely, drawing on the original-language text, " +
                $"historical context, and cultural background where relevant. " +
                $"Format your response in clear markdown.";

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
                var errorTask  = process.StandardError.ReadToEndAsync();

                // Wait up to 30 seconds for the CLI to return
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                await process.WaitForExitAsync(cts.Token);

                var output = await outputTask;
                var error  = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("AGY CLI failed (exit {Code}) for verse note AI: {Error}",
                        process.ExitCode, error);
                    throw new InvalidOperationException($"CLI execution failed: {error}");
                }

                return output?.Trim() ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AGY CLI timed out or was cancelled for verse note AI on {Reference}", reference);
                if (!process.HasExited) process.Kill();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AGY CLI for verse note AI on {Reference}", reference);
                throw;
            }
            finally
            {
                // Deactivate this transient worker grain after job is complete to free memory
                DeactivateOnIdle();
            }
        }
    }
}
