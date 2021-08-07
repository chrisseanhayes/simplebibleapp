using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TransChangeSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "transChange";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.StartNewPart(VersePartStateType.TransChange, reader);
        }
    }
}