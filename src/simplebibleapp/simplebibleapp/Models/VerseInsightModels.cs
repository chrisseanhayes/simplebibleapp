namespace simplebibleapp.Models
{
    public class VerseInsightRequest
    {
        public string Reference { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
    }

    public class VerseInsightViewModel
    {
        public string Reference { get; set; } = string.Empty;
        public string RawMarkdown { get; set; } = string.Empty;
        public string RenderedHtml { get; set; } = string.Empty;
    }
}
