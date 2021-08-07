namespace simplebibleapp.xmlbiblerepository
{
    public abstract class VersePartBase : IVersePart
    {
        public abstract VersePartType Type { get; }
        public int SortOrder { get; }

        public abstract string Text { get; }

        public VersePartBase(int sortOrder)
        {
            SortOrder = sortOrder;
        }

        public override string ToString()
        {
            return $"Text: {Text}; Type: {Type}";
        }
    }
}