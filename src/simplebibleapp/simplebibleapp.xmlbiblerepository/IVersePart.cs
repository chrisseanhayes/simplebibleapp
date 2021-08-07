namespace simplebibleapp.xmlbiblerepository
{
    public interface IVersePart
    {
        string Text { get; }
        VersePartType Type { get; }
        int SortOrder { get; }
    }
}