using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TitleBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "title";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.OpenTitle();
        }
    }
}