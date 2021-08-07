using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class WhiteSpaceBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Whitespace;
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.AddText(reader.Value);
        }
    }
}