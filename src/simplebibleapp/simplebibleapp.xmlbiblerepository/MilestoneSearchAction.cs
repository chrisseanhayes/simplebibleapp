using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class MilestoneSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "milestone";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.StartNewPart(VersePartStateType.Milestone, reader);
        }
    }
}