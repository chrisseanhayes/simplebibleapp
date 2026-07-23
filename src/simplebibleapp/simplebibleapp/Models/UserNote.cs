using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace simplebibleapp.Models
{
    public enum NoteType
    {
        Personal = 0,
        AiInsight = 1
    }

    /// <summary>
    /// A personal study note attached by a logged-in user to a specific Bible verse.
    /// Can be a personal hand-written note or an AI-generated answer to a prompt.
    /// </summary>
    public class UserNote
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Foreign key to the AspNetUsers table.</summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>Book abbreviation, e.g. "John".</summary>
        [Required]
        [MaxLength(20)]
        public string BookAbbr { get; set; }

        [Required]
        public int Chapter { get; set; }

        [Required]
        public int Verse { get; set; }

        /// <summary>Personal or AI-generated note content (plain text or markdown).</summary>
        [Required]
        public string NoteText { get; set; }

        /// <summary>
        /// For AI notes: the question the user asked. Stored so they know what was queried.
        /// Null for personal notes.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>Whether this is a personal note or an AI-answered note.</summary>
        public NoteType NoteType { get; set; } = NoteType.Personal;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation property ──────────────────────────────────────────────
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}
