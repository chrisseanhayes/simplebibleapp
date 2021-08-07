using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class WordElementBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "w";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.StartNewPart(VersePartStateType.Word, reader);
        }
    }
}