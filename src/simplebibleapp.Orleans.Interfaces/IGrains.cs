using System;
using System.Threading.Tasks;

namespace simplebibleapp.Orleans.Interfaces
{
    /// <summary>
    /// A global grain keyed by a scripture reference (e.g., "John 1:1")
    /// that guarantees only one AI generation happens globally for a given verse.
    /// </summary>
    public interface IVerseInsightGrain : IGrainWithStringKey
    {
        Task<string> GetOrGenerateInsightAsync(string reference, string text);
        Task<string?> GetInsightAsync(string reference);
    }

    /// <summary>
    /// A transient worker grain keyed by a unique Job ID (Guid)
    /// that processes a specific user's custom question.
    /// </summary>
    public interface IVerseNoteJobGrain : IGrainWithGuidKey
    {
        Task<string> GenerateNoteAsync(string reference, string question);
    }
}
