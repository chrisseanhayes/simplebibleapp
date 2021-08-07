namespace simplebibleapp.xmlbiblerepository
{
    public class WordTextPart
    {
        public VerseWordSubpartType Type { get; }
        public string Value { get; }

        public WordTextPart(VerseWordSubpartType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}