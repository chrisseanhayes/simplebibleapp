using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class SegSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "seg";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.InsertSubPart(VerseWordSubpartType.Seg, reader);
        }
    }
}