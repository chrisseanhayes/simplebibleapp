using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class NoteEndSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.EndElement &&
                   reader.Name == "note";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.ClosePart();
        }
    }
}