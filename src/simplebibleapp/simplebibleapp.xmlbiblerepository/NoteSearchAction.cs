using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class NoteSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "note";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.StartNewPart(VersePartStateType.Note, reader);
        }
    }
}