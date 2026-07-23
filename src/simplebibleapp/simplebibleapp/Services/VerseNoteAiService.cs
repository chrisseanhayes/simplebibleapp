using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace simplebibleapp.Services
{
    /// <summary>
    /// Sends a user's free-form question about a specific verse to the AGY CLI
    /// and returns the answer as a markdown string.
    /// </summary>
    public interface IVerseNoteAiService
    {
        Task<string> AskAboutVerseAsync(
            string verseReference,
            string userQuestion,
            CancellationToken cancellationToken = default);
    }

    public class VerseNoteAiService : IVerseNoteAiService
    {
        private readonly ILogger<VerseNoteAiService> _logger;
        private readonly string _cliExecutablePath;

        public VerseNoteAiService(IConfiguration configuration, ILogger<VerseNoteAiService> logger)
        {
            _logger = logger;
            _cliExecutablePath = configuration["AgyCli:Path"] ?? "agy";
        }

        public async Task<string> AskAboutVerseAsync(
            string verseReference,
            string userQuestion,
            CancellationToken cancellationToken = default)
        {
            // Build a focused prompt that gives the AI the verse context + the user's question
            var prompt =
                $"Bible verse context: {verseReference} (KJV / Textus Receptus / Masoretic). " +
                $"User question: \"{userQuestion}\". " +
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
            startInfo.ArgumentList.Add("flash");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(prompt);

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask  = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cancellationToken);

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
                _logger.LogWarning("AGY CLI cancelled for verse note AI on {Reference}", verseReference);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AGY CLI for verse note AI on {Reference}", verseReference);
                throw;
            }
        }
    }
}
