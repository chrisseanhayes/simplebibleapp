using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using simplebibleapp.Data;
using simplebibleapp.Hubs;
using simplebibleapp.Models;
using simplebibleapp.Orleans.Interfaces;

namespace simplebibleapp.Controllers
{
    /// <summary>
    /// REST API for authenticated users to manage their personal Bible study notes
    /// (hand-written and AI-generated).
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Azure.Storage.Blobs.BlobServiceClient _blobServiceClient;

        public NotesController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IServiceScopeFactory serviceScopeFactory,
            Azure.Storage.Blobs.BlobServiceClient blobServiceClient)
        {
            _db = db;
            _userManager = userManager;
            _serviceScopeFactory = serviceScopeFactory;
            _blobServiceClient = blobServiceClient;
        }

        // ── DTOs ────────────────────────────────────────────────────────────

        public record NoteDto(
            int Id,
            string BookAbbr,
            int Chapter,
            int Verse,
            string NoteText,
            string Prompt,
            string NoteType,
            DateTime UpdatedAt,
            string RenderedHtml = null);

        public record UpsertNoteRequest(string BookAbbr, int Chapter, int Verse, string NoteText);

        public record AskAiRequest(
            string BookAbbr,
            int Chapter,
            int Verse,
            string Reference,   // e.g. "John 3:16"
            string Question,
            string ConnectionId);

        // ── Blob Helpers ────────────────────────────────────────────────────
        private async Task<string> ResolveNoteTextAsync(string dbText)
        {
            if (!string.IsNullOrEmpty(dbText) && dbText.StartsWith("blob:"))
            {
                var blobName = dbText.Substring(5);
                var container = _blobServiceClient.GetBlobContainerClient("usernotes");
                var blob = container.GetBlobClient(blobName);
                if (await blob.ExistsAsync())
                {
                    var result = await blob.DownloadContentAsync();
                    return result.Value.Content.ToString();
                }
                return string.Empty;
            }
            return dbText;
        }

        private async Task<string> SaveToBlobAsync(string text, string oldDbText = null)
        {
            var blobName = (!string.IsNullOrEmpty(oldDbText) && oldDbText.StartsWith("blob:")) 
                           ? oldDbText.Substring(5) 
                           : $"note_{Guid.NewGuid():N}.md";

            var container = _blobServiceClient.GetBlobContainerClient("usernotes");
            var blob = container.GetBlobClient(blobName);
            
            using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
            await blob.UploadAsync(stream, overwrite: true);

            return "blob:" + blobName;
        }

        // ── GET /api/Notes?bookAbbr=John&chapter=1 ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetNotes([FromQuery] string bookAbbr, [FromQuery] int chapter)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(bookAbbr) || chapter <= 0)
                return BadRequest(new { error = "bookAbbr and chapter are required." });

            var notesFromDb = await _db.UserNotes
                .Where(n => n.UserId == userId && n.BookAbbr == bookAbbr && n.Chapter == chapter)
                .OrderBy(n => n.Verse).ThenBy(n => n.NoteType).ThenBy(n => n.CreatedAt)
                .ToListAsync();

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var notes = new List<NoteDto>();
            foreach (var n in notesFromDb)
            {
                var text = await ResolveNoteTextAsync(n.NoteText);
                notes.Add(new NoteDto(
                    n.Id, n.BookAbbr, n.Chapter, n.Verse, text, n.Prompt, n.NoteType.ToString(), n.UpdatedAt,
                    n.NoteType == NoteType.AiInsight ? Markdown.ToHtml(text, pipeline) : null
                ));
            }

            return Ok(notes);
        }

        // ── GET /api/Notes/all ───────────────────────────────────────────────
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var notesFromDb = await _db.UserNotes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var notes = new List<NoteDto>();
            foreach (var n in notesFromDb)
            {
                var text = await ResolveNoteTextAsync(n.NoteText);
                notes.Add(new NoteDto(
                    n.Id, n.BookAbbr, n.Chapter, n.Verse, text, n.Prompt, n.NoteType.ToString(), n.UpdatedAt,
                    n.NoteType == NoteType.AiInsight ? Markdown.ToHtml(text, pipeline) : null
                ));
            }

            return Ok(notes);
        }

        // ── POST /api/Notes (upsert personal note) ───────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpsertNote([FromBody] UpsertNoteRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (req == null || string.IsNullOrWhiteSpace(req.BookAbbr) || req.Chapter <= 0 || req.Verse <= 0)
                return BadRequest(new { error = "BookAbbr, Chapter, and Verse are required." });

            // Personal notes: one per verse per user
            var existing = await _db.UserNotes.FirstOrDefaultAsync(n =>
                n.UserId == userId &&
                n.BookAbbr == req.BookAbbr &&
                n.Chapter == req.Chapter &&
                n.Verse == req.Verse &&
                n.NoteType == NoteType.Personal);

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(req.NoteText))
                {
                    _db.UserNotes.Remove(existing);
                    await _db.SaveChangesAsync();
                    return Ok(new { deleted = true, id = existing.Id });
                }

                existing.NoteText = await SaveToBlobAsync(req.NoteText, existing.NoteText);
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                var text = await ResolveNoteTextAsync(existing.NoteText);
                return Ok(new NoteDto(existing.Id, existing.BookAbbr, existing.Chapter, existing.Verse, text, null, "Personal", existing.UpdatedAt, null));
            }

            if (string.IsNullOrWhiteSpace(req.NoteText))
                return Ok(new { deleted = false });

            var blobUri = await SaveToBlobAsync(req.NoteText);
            var note = new UserNote
            {
                UserId = userId,
                BookAbbr = req.BookAbbr,
                Chapter = req.Chapter,
                Verse = req.Verse,
                NoteText = blobUri,
                NoteType = NoteType.Personal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserNotes.Add(note);
            await _db.SaveChangesAsync();

            return Ok(new NoteDto(note.Id, note.BookAbbr, note.Chapter, note.Verse, req.NoteText, null, "Personal", note.UpdatedAt, null));
        }

        // ── POST /api/Notes/AskAi ────────────────────────────────────────────
        /// <summary>
        /// Fire-and-forget: calls the AGY CLI with the user's question about a verse,
        /// saves the answer as an AiInsight note, then pushes it back via SignalR.
        /// </summary>
        [HttpPost("AskAi")]
        public IActionResult AskAi([FromBody] AskAiRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (req == null
                || string.IsNullOrWhiteSpace(req.Reference)
                || string.IsNullOrWhiteSpace(req.Question)
                || string.IsNullOrWhiteSpace(req.ConnectionId))
                return BadRequest(new { error = "Reference, Question, and ConnectionId are required." });

            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LinguisticHub>>();
                var orleansClient = scope.ServiceProvider.GetRequiredService<IClusterClient>();
                var db         = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger     = scope.ServiceProvider.GetRequiredService<ILogger<NotesController>>();

                try
                {
                    var grain = orleansClient.GetGrain<IVerseNoteJobGrain>(Guid.NewGuid());
                    var markdown = await grain.GenerateNoteAsync(req.Reference, req.Question);

                    if (string.IsNullOrWhiteSpace(markdown))
                    {
                        await hubContext.Clients.Client(req.ConnectionId)
                            .SendAsync("ReceiveAiNoteError", new { error = "AI returned an empty response." });
                        return;
                    }

                    // Render to HTML for display
                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    var html = Markdown.ToHtml(markdown, pipeline);

                    // Save markdown to Blob Storage
                    var blobServiceClient = scope.ServiceProvider.GetRequiredService<Azure.Storage.Blobs.BlobServiceClient>();
                    var container = blobServiceClient.GetBlobContainerClient("usernotes");
                    var blobName = $"note_{Guid.NewGuid():N}.md";
                    var blob = container.GetBlobClient(blobName);
                    using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));
                    await blob.UploadAsync(stream, overwrite: true);

                    // Persist as an AiInsight note (multiple allowed per verse)
                    var note = new UserNote
                    {
                        UserId    = userId,
                        BookAbbr  = req.BookAbbr,
                        Chapter   = req.Chapter,
                        Verse     = req.Verse,
                        NoteText  = "blob:" + blobName,
                        Prompt    = req.Question,
                        NoteType  = NoteType.AiInsight,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.UserNotes.Add(note);
                    await db.SaveChangesAsync();

                    var dto = new NoteDto(note.Id, note.BookAbbr, note.Chapter, note.Verse, markdown, req.Question, "AiInsight", note.UpdatedAt, html);

                    await hubContext.Clients.Client(req.ConnectionId)
                        .SendAsync("ReceiveAiNote", new
                        {
                            note = dto,
                            renderedHtml = html
                        });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AskAi background task failed for {Reference}", req.Reference);
                    try
                    {
                        await hubContext.Clients.Client(req.ConnectionId)
                            .SendAsync("ReceiveAiNoteError", new { error = "An error occurred generating the AI note." });
                    }
                    catch { }
                }
            });

            return Ok(new { status = "processing" });
        }

        // ── DELETE /api/Notes/{id} ───────────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var note = await _db.UserNotes.FindAsync(id);
            if (note == null || note.UserId != userId)
                return NotFound();

            _db.UserNotes.Remove(note);
            await _db.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
