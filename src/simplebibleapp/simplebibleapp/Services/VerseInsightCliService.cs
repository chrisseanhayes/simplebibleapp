using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace simplebibleapp.Services
{
    public interface IVerseInsightCliService
    {
        Task<string> GetVerseInsightAsync(string verseReference, CancellationToken cancellationToken = default);
    }

    public class VerseInsightCliService : IVerseInsightCliService
    {
        private readonly ILogger<VerseInsightCliService> _logger;
        private readonly string _cliExecutablePath;

        public VerseInsightCliService(IConfiguration configuration, ILogger<VerseInsightCliService> logger)
        {
            _logger = logger;
            _cliExecutablePath = configuration["AgyCli:Path"] ?? "agy";
        }

        public async Task<string> GetVerseInsightAsync(string verseReference, CancellationToken cancellationToken = default)
        {
            var prompt = $"using kjv, textus receptus, masoretic, septuagint links new to old testament. " +
                         $"Provide insights, context, culture, archaic words, links to old testament, literary nuances for {verseReference}. " +
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
            startInfo.ArgumentList.Add("flash");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(prompt);

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cancellationToken);

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("AGY CLI failed with code {ExitCode}: {Error}", process.ExitCode, error);
                    throw new InvalidOperationException($"CLI execution failed: {error}");
                }

                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AGY CLI for reference {Reference}", verseReference);
                throw;
            }
        }
    }
}
