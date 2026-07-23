using System;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using simplebibleapp.Hubs;
using simplebibleapp.Models;
using Orleans;
using simplebibleapp.Orleans.Interfaces;

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
            var orleansClient = scope.ServiceProvider.GetRequiredService<IClusterClient>();
            var grain = orleansClient.GetGrain<IVerseInsightGrain>(reference);

            var cachedMarkdown = await grain.GetInsightAsync(reference);

            if (!string.IsNullOrEmpty(cachedMarkdown))
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                var html = Markdown.ToHtml(cachedMarkdown, pipeline);
                
                var viewModel = new VerseInsightViewModel
                {
                    Reference = reference,
                    RenderedHtml = html,
                    RawMarkdown = cachedMarkdown
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
                var orleansClient = scope.ServiceProvider.GetRequiredService<IClusterClient>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ScriptureController>>();

                try
                {
                    var grain = orleansClient.GetGrain<IVerseInsightGrain>(request.Reference);
                    var markdownResponse = await grain.GetOrGenerateInsightAsync(request.Reference, request.Reference);

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
