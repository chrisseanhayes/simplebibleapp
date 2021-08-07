using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TransChangeEndSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.EndElement &&
                   reader.Name == "transChange";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.ClosePart();
        }
    }
}