using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace simplebibleapp.Models
{
    /// <summary>
    /// A personal study note attached by a logged-in user to a specific Bible verse.
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

        /// <summary>Free-form note text (plain text or markdown).</summary>
        [Required]
        public string NoteText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation property ──────────────────────────────────────────────
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}
