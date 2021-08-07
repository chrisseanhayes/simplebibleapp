using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class ChapterEndSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "chapter" &&
                   builder.ChapterOpen;
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.CloseBuilder();
        }
    }
}