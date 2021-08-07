using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TextBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Text;
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.AddText(reader.Value);
        }
    }
}