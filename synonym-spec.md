# ASP.NET Core Tech Spec: `Agy` Original-Language Synonym Engine

## 1. Architectural Overview

```
[ Front-End / Web API Controller ]
              │
              ▼
  [ IAgyLinguisticService ]  ◄─── Checks Local Cache / DB First
              │ (Cache Miss)
              ▼
     [ GeminiCliService ]   ───► Invokes `gemini --output-format json` process
              │
              ▼
   [ System.Text.Json ]     ───► Deserializes `.response` payload
              │
              ▼
 [ Database / Response DTO ] ───► In-Memory Cache + Persistent Store

```

---

## 2. Domain Models & DTOs

Create a dedicated C# namespace for the payload DTOs matching the JSON contract.

```csharp
namespace Agy.LinguisticEngine.Models;

using System.Text.Json.Serialization;

public record TargetSelectionDto(
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("anchor_strongs")] string AnchorStrongs,
    [property: JsonPropertyName("anchor_lemma")] string AnchorLemma,
    [property: JsonPropertyName("transliteration")] string Transliteration,
    [property: JsonPropertyName("contextual_gloss")] string ContextualGloss
);

public record ExegeticalContextDto(
    [property: JsonPropertyName("immediate_context_role")] string ImmediateContextRole,
    [property: JsonPropertyName("local_thematic_function")] string LocalThematicFunction,
    [property: JsonPropertyName("canonical_trajectory_summary")] string CanonicalTrajectorySummary
);

public record SynonymNodeDto(
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("strongs")] string Strongs,
    [property: JsonPropertyName("lemma")] string Lemma,
    [property: JsonPropertyName("transliteration")] string Transliteration,
    [property: JsonPropertyName("primary_gloss")] string PrimaryGloss,
    [property: JsonPropertyName("semantic_domain")] string SemanticDomain,
    [property: JsonPropertyName("relationship")] string Relationship,
    [property: JsonPropertyName("contextual_confidence")] double ContextualConfidence,
    [property: JsonPropertyName("thematic_alignment_notes")] string ThematicAlignmentNotes
);

public record AgyLinguisticPayloadDto(
    [property: JsonPropertyName("target_selection")] TargetSelectionDto TargetSelection,
    [property: JsonPropertyName("exegetical_context")] ExegeticalContextDto ExegeticalContext,
    [property: JsonPropertyName("synonym_network")] List<SynonymNodeDto> SynonymNetwork
);

// Wrapper object returned by Gemini CLI
internal record GeminiCliWrapper(
    [property: JsonPropertyName("response")] string Response
);

```

---

## 3. The `GeminiCliService` Implementation

This service manages process lifetime, escapes CLI arguments, and handles output stream buffers asynchronously.

### Service Interface

```csharp
namespace Agy.LinguisticEngine.Services;

public interface IAgyLinguisticService
{
    Task<AgyLinguisticPayloadDto?> AnalyzeTokenAsync(
        string reference, 
        string anchorStrongs, 
        string anchorLemma, 
        string language, 
        CancellationToken cancellationToken = default);
}

```

### Implementation

```csharp
namespace Agy.LinguisticEngine.Services;

using System.Diagnostics;
using System.Text.Json;
using Agy.LinguisticEngine.Models;

public class GeminiCliService : IAgyLinguisticService
{
    private readonly ILogger<GeminiCliService> _logger;

    public GeminiCliService(ILogger<GeminiCliService> logger)
    {
        _logger = logger;
    }

    public async Task<AgyLinguisticPayloadDto?> AnalyzeTokenAsync(
        string reference,
        string anchorStrongs,
        string anchorLemma,
        string language,
        CancellationToken cancellationToken = default)
    {
        string prompt = BuildPromptPayload(reference, anchorStrongs, anchorLemma, language);

        var startInfo = new ProcessStartInfo
        {
            FileName = "gemini",
            ArgumentList = { "--output-format", "json", "-p", prompt },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Gemini CLI process failed with code {ExitCode}: {StdErr}", process.ExitCode, stderr);
                return null;
            }

            // Deserialization Step 1: Parse top-level CLI wrapper
            var wrapper = JsonSerializer.Deserialize<GeminiCliWrapper>(stdout);
            if (string.IsNullOrWhiteSpace(wrapper?.Response))
            {
                _logger.LogWarning("Gemini CLI returned an empty response wrapper.");
                return null;
            }

            // Deserialization Step 2: Parse payload string contained inside '.response'
            var payload = JsonSerializer.Deserialize<AgyLinguisticPayloadDto>(wrapper.Response);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Gemini CLI analysis for {Strongs} in {Reference}", anchorStrongs, reference);
            throw;
        }
    }

    private static string BuildPromptPayload(string reference, string anchorStrongs, string anchorLemma, string language)
    {
        return $"""
        Target Selection:
        - Reference: {reference}
        - Anchor Strong's: {anchorStrongs}
        - Anchor Lemma: {anchorLemma}
        - Language: {language}

        Analyze the target token in its immediate verse context and across the biblical canon (MT / TR / LXX). Return ONLY valid JSON adhering strictly to this schema:

        {{
          "target_selection": {{
            "reference": "string",
            "anchor_strongs": "string",
            "anchor_lemma": "string",
            "transliteration": "string",
            "contextual_gloss": "string"
          }},
          "exegetical_context": {{
            "immediate_context_role": "string",
            "local_thematic_function": "string",
            "canonical_trajectory_summary": "string"
          }},
          "synonym_network": [
            {{
              "language": "Hebrew | Greek",
              "strongs": "string",
              "lemma": "string",
              "transliteration": "string",
              "primary_gloss": "string",
              "semantic_domain": "string",
              "relationship": "direct_synonym | lxx_translation_equivalent | semantic_neighbor | antonym",
              "contextual_confidence": 0.95,
              "thematic_alignment_notes": "string"
            }}
          ]
        }}
        """;
    }
}

```

---

## 4. Web API Controller / Endpoint Specification

Expose an endpoint (Minimal API or Controller) for your frontend or UI engine to trigger.

### Minimal API Endpoint (`Program.cs`)

```csharp
builder.Services.AddScoped<IAgyLinguisticService, GeminiCliService>();

var app = builder.Build();

app.MapGet("/api/linguistics/synonyms", async (
    string reference, 
    string strongs, 
    string lemma, 
    string language, 
    IAgyLinguisticService agyService,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(strongs))
    {
        return Results.BadRequest("Reference and Strongs parameters are required.");
    }

    var result = await agyService.AnalyzeTokenAsync(reference, strongs, lemma, language, ct);
    
    return result is not null 
        ? Results.Ok(result) 
        : Results.Problem("Failed to analyze original language token.", statusCode: 500);
});

```

---

## 5. Caching & Performance (Recommended ASP.NET Pattern)

Because CLI subprocess calls introduce execution latency (1–3 seconds), wrap `GeminiCliService` using the **Decorator Pattern** with `.NET`'s `IMemoryCache` or `IDistributedCache`:

```csharp
public class CachedAgyLinguisticService : IAgyLinguisticService
{
    private readonly IAgyLinguisticService _innerService;
    private readonly IMemoryCache _cache;

    public CachedAgyLinguisticService(IAgyLinguisticService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }

    public async Task<AgyLinguisticPayloadDto?> AnalyzeTokenAsync(
        string reference, string anchorStrongs, string anchorLemma, string language, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"agy:{anchorStrongs}:{reference.Replace(" ", "_")}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30); // Long-lived lexical cache
            return await _innerService.AnalyzeTokenAsync(reference, anchorStrongs, anchorLemma, language, cancellationToken);
        });
    }
}

```