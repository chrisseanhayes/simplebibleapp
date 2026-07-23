using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using simplebibleapp.Data;
using simplebibleapp.Models;

namespace simplebibleapp.Controllers
{
    /// <summary>
    /// REST API for authenticated users to manage their personal Bible study notes.
    /// All endpoints require authentication.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── DTOs ────────────────────────────────────────────────────────────

        public record NoteDto(int Id, string BookAbbr, int Chapter, int Verse, string NoteText, DateTime UpdatedAt);
        public record UpsertNoteRequest(string BookAbbr, int Chapter, int Verse, string NoteText);

        // ── GET /api/Notes?bookAbbr=John&chapter=1 ───────────────────────
        [HttpGet]
        public async Task<IActionResult> GetNotes([FromQuery] string bookAbbr, [FromQuery] int chapter)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(bookAbbr) || chapter <= 0)
                return BadRequest(new { error = "bookAbbr and chapter are required." });

            var notes = await _db.UserNotes
                .Where(n => n.UserId == userId && n.BookAbbr == bookAbbr && n.Chapter == chapter)
                .Select(n => new NoteDto(n.Id, n.BookAbbr, n.Chapter, n.Verse, n.NoteText, n.UpdatedAt))
                .ToListAsync();

            return Ok(notes);
        }

        // ── GET /api/Notes/all ───────────────────────────────────────────────
        /// <summary>Returns all notes for the authenticated user.</summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var notes = await _db.UserNotes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.UpdatedAt)
                .Select(n => new NoteDto(n.Id, n.BookAbbr, n.Chapter, n.Verse, n.NoteText, n.UpdatedAt))
                .ToListAsync();

            return Ok(notes);
        }

        // ── POST /api/Notes (upsert) ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpsertNote([FromBody] UpsertNoteRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (req == null || string.IsNullOrWhiteSpace(req.BookAbbr) || req.Chapter <= 0 || req.Verse <= 0)
                return BadRequest(new { error = "BookAbbr, Chapter, and Verse are required." });

            var existing = await _db.UserNotes.FirstOrDefaultAsync(n =>
                n.UserId == userId &&
                n.BookAbbr == req.BookAbbr &&
                n.Chapter == req.Chapter &&
                n.Verse == req.Verse);

            if (existing != null)
            {
                // Update
                if (string.IsNullOrWhiteSpace(req.NoteText))
                {
                    // Empty text ⟹ delete the note
                    _db.UserNotes.Remove(existing);
                    await _db.SaveChangesAsync();
                    return Ok(new { deleted = true });
                }

                existing.NoteText = req.NoteText;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new NoteDto(existing.Id, existing.BookAbbr, existing.Chapter, existing.Verse, existing.NoteText, existing.UpdatedAt));
            }

            if (string.IsNullOrWhiteSpace(req.NoteText))
                return Ok(new { deleted = false });

            // Create
            var note = new UserNote
            {
                UserId = userId,
                BookAbbr = req.BookAbbr,
                Chapter = req.Chapter,
                Verse = req.Verse,
                NoteText = req.NoteText,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserNotes.Add(note);
            await _db.SaveChangesAsync();

            return Ok(new NoteDto(note.Id, note.BookAbbr, note.Chapter, note.Verse, note.NoteText, note.UpdatedAt));
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
