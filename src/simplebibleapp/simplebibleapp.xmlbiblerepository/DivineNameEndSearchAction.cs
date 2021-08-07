using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class DivineNameEndSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.EndElement &&
                   reader.Name == "divineName";
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.EndSubPart(VerseWordSubpartType.DivineName);
        }
    }
}