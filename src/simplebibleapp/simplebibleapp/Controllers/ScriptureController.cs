using System;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using simplebibleapp.Hubs;
using simplebibleapp.Models;
using simplebibleapp.Services;

namespace simplebibleapp.Controllers
{
    public class ScriptureController : Controller
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ScriptureController(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpGet]
        public async Task<IActionResult> CheckInsightCache(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return BadRequest(new { error = "Reference is required." });

            using var scope = _serviceScopeFactory.CreateScope();
            var l1 = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            var l2 = scope.ServiceProvider.GetRequiredService<VerseInsightCache>();

            string l1Key = $"verse_insight:{reference}";
            string? cachedMarkdown = null;

            if (l1.TryGetValue(l1Key, out string? l1Cached) && !string.IsNullOrEmpty(l1Cached))
            {
                cachedMarkdown = l1Cached;
            }
            else
            {
                var l2Cached = await l2.GetAsync(reference, CancellationToken.None);
                if (!string.IsNullOrEmpty(l2Cached))
                {
                    cachedMarkdown = l2Cached;
                    // Warm L1
                    l1.Set(l1Key, l2Cached, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(24)
                    });
                }
            }

            if (!string.IsNullOrEmpty(cachedMarkdown))
            {
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();

                var htmlContent = Markdown.ToHtml(cachedMarkdown, pipeline);

                var viewModel = new VerseInsightViewModel
                {
                    Reference = reference,
                    RawMarkdown = cachedMarkdown,
                    RenderedHtml = htmlContent
                };

                return Json(new { cached = true, data = viewModel });
            }

            return Json(new { cached = false });
        }

        [HttpPost]
        public IActionResult GetInsight([FromBody] VerseInsightRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Reference))
            {
                return BadRequest(new { error = "Verse reference is required." });
            }
            
            if (string.IsNullOrWhiteSpace(request?.ConnectionId))
            {
                return BadRequest(new { error = "ConnectionId is required." });
            }

            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LinguisticHub>>();
                var agyCliService = scope.ServiceProvider.GetRequiredService<IVerseInsightCliService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ScriptureController>>();

                try
                {
                    var markdownResponse = await agyCliService.GetVerseInsightAsync(request.Reference, CancellationToken.None);

                    if (!string.IsNullOrEmpty(markdownResponse))
                    {
                        await hubContext.Clients.Client(request.ConnectionId)
                            .SendAsync("ReceiveVerseInsightReady", request.Reference);
                    }
                    else
                    {
                        await hubContext.Clients.Client(request.ConnectionId)
                            .SendAsync("ReceiveVerseInsightError", request.Reference, "No insight generated.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching insight via SignalR background task");
                    await hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveVerseInsightError", request.Reference, "An error occurred while generating insight.");
                }
            });

            return Json(new { status = "processing" });
        }
    }
}
