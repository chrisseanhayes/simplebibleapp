using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TitleEndBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   builder.TitleOpen &&
                   reader.NodeType == XmlNodeType.EndElement &&
                   reader.Name == "title";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.CloseTitle();
        }
    }
}