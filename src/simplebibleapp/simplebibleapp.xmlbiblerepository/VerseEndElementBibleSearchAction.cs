using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class VerseEndElementBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "verse" &&
                   reader.GetAttribute("eID") != null;
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.CloseCurrentVerse();
        }
    }
}