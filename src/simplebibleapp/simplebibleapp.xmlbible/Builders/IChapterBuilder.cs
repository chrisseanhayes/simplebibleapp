namespace simplebibleapp.xmlbible
{
    public interface IChapterBuilder
    {
        NodeBase CurrentNode { get; }
        bool HasNextChapter { get; }
        bool HasPrevChapter { get; }
        string NextBookAbbr { get; }
        int NextChapterNumber { get; }
        string PrevBookAbbr { get; }
        int PrevChapterNumber { get; }

        BuilderState GetChapter(string bookAbbreviation, int chapter);
    }
}