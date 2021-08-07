namespace simplebibleapp.xmlbiblerepository
{
    public class VerseText : VersePartBase
    {
        public override VersePartType Type { get; }

        private readonly string _text;
        public bool HasText => _text != null;
        public override string Text => _text;

        public VerseText(int sortOrder, string text) : base(sortOrder)
        {
            _text = text;
            Type = string.IsNullOrWhiteSpace(text) ? VersePartType.Whitespace : VersePartType.Text;
        }

    }
}