# Technical Specification: Scripture Insight System (`Mark 1` Integration)

This document provides an end-to-end implementation specification for integrating the Scripture Insight workflow into an **ASP.NET Core MVC** application. The system invokes an external CLI tool (`agy cli`) to fetch structured Markdown commentary for requested Bible passages, then parses and renders the response in the UI.

---

## 1. Architectural Overview

```
+------------------+         HTTP POST /Scripture/Insight         +-------------------------+
|                  | -------------------------------------------> |                         |
|  Browser (UI)    |                                              |  ScriptureController    |
|                  | <------------------------------------------- |                         |
+------------------+             JSON / HTML Payload              +-------------------------+
                                                                               |
                                                                               v
                                                                  +-------------------------+
                                                                  |  IAgyCliService         |
                                                                  +-------------------------+
                                                                               |
                                                                  Process.Start("agy")
                                                                               v
                                                                  +-------------------------+
                                                                  |  External AGY CLI Tool  |
                                                                  +-------------------------+

```

---

## 2. CLI Execution Service

### Interface Definition

```csharp
namespace ScriptureApp.Services
{
    public interface IAgyCliService
    {
        Task<string> GetVerseInsightAsync(string verseReference, CancellationToken cancellationToken = default);
    }
}

```

### Implementation Details

```csharp
using System.Diagnostics;
using System.Text;

namespace ScriptureApp.Services
{
    public class AgyCliService : IAgyCliService
    {
        private readonly ILogger<AgyCliService> _logger;
        private readonly string _cliExecutablePath;

        public AgyCliService(IConfiguration configuration, ILogger<AgyCliService> logger)
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
                Arguments = $"query \"{prompt.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

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

```

---

## 3. Data Transfer Objects & ViewModel

```csharp
namespace ScriptureApp.Models
{
    public class VerseInsightRequest
    {
        public string Reference { get; set; } = "Mark 1:1";
    }

    public class VerseInsightViewModel
    {
        public string Reference { get; set; } = string.Empty;
        public string RawMarkdown { get; set; } = string.Empty;
        public string RenderedHtml { get; set; } = string.Empty;
    }
}

```

---

## 4. Controller Implementation

```csharp
using Microsoft.AspNetCore.Mvc;
using ScriptureApp.Models;
using ScriptureApp.Services;
using Markdig;

namespace ScriptureApp.Controllers
{
    public class ScriptureController : Controller
    {
        private readonly IAgyCliService _agyCliService;

        public ScriptureController(IAgyCliService agyCliService)
        {
            _agyCliService = agyCliService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new VerseInsightViewModel { Reference = "Mark 1:1" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetInsight([FromBody] VerseInsightRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Reference))
            {
                return BadRequest("Verse reference is required.");
            }

            var markdownResponse = await _agyCliService.GetVerseInsightAsync(request.Reference, cancellationToken);

            // Configure Markdig pipeline for tables, blockquotes, and advanced formatting
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var htmlContent = Markdown.ToHtml(markdownResponse, pipeline);

            return Json(new VerseInsightViewModel
            {
                Reference = request.Reference,
                RawMarkdown = markdownResponse,
                RenderedHtml = htmlContent
            });
        }
    }
}

```

---

## 5. Razor View (`Views/Scripture/Index.cshtml`)

```html
@model ScriptureApp.Models.VerseInsightViewModel

@{
    ViewData["Title"] = "Scripture Insights";
}

<div class="container mt-4">
    <h2>Scripture Insight Engine</h2>
    <p class="text-muted">KJV / Textus Receptus / Masoretic / LXX Analysis</p>

    <div class="row mb-3">
        <div class="col-md-6">
            <div class="input-group">
                <input type="text" id="verseReference" class="form-control" value="@Model.Reference" placeholder="e.g. Mark 1:1, Mark 1:14-15" />
                <button class="btn btn-primary" id="btnFetchInsight" type="button">
                    Get Insight
                </button>
            </div>
        </div>
    </div>

    <div id="loadingSpinner" class="d-none my-4">
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading CLI insights...</span>
        </div>
        <span class="ms-2 text-muted">Running AGY CLI process...</span>
    </div>

    <div id="insightContainer" class="card shadow-sm mt-4 d-none">
        <div class="card-header bg-dark text-white">
            <h5 class="card-title mb-0" id="insightTitle">Insight Output</h5>
        </div>
        <div class="card-body" id="insightBody">
            <!-- Rendered Markdown Output -->
        </div>
    </div>
</div>

@section Scripts {
    <script>
        document.getElementById('btnFetchInsight').addEventListener('click', async () => {
            const reference = document.getElementById('verseReference').value.trim();
            if (!reference) return;

            const spinner = document.getElementById('loadingSpinner');
            const container = document.getElementById('insightContainer');
            const body = document.getElementById('insightBody');
            const title = document.getElementById('insightTitle');

            spinner.classList.remove('d-none');
            container.classList.add('d-none');

            try {
                const response = await fetch('/Scripture/GetInsight', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({ reference: reference })
                });

                if (!response.ok) throw new Error('Network response was not ok.');

                const data = await response.json();
                title.innerText = `Insight: ${data.reference}`;
                body.innerHTML = data.renderedHtml;
                container.classList.remove('d-none');
            } catch (err) {
                alert('Error fetching insight: ' + err.message);
            } finally {
                spinner.classList.add('d-none');
            }
        });
    </script>
}

```

---

## 6. Dependency Injection & Configuration

### `appsettings.json`

```json
{
  "AgyCli": {
    "Path": "C:\\Tools\\agy.exe"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```

### `Program.cs` Registration

```csharp
using ScriptureApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Services
builder.Services.AddControllersWithViews();

// Register CLI Service
builder.Services.AddScoped<IAgyCliService, AgyCliService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Scripture}/{action=Index}/{id?}");

app.Run();

```

---

## 7. Required NuGet Packages

Install the following packages via Package Manager Console or `.csproj`:

```bash
dotnet add package Markdig

```