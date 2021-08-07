using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class WordEndElementBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.EndElement &&
                   reader.Name == "w";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.ClosePart();
        }
    }
}