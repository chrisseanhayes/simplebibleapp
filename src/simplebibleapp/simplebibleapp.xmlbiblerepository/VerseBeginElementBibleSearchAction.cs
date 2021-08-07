using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class VerseBeginElementBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return builder.ChapterOpen &&
                   reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "verse" &&
                   reader.GetAttribute("sID") != null;
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            string sid = null;
            string osisId = null;
            if (reader.MoveToAttribute("sID"))
            {
                sid = reader.Value;
                reader.MoveToElement();
            }
            if (reader.MoveToAttribute("osisID"))
            {
                osisId = reader.Value;
                reader.MoveToElement();
            }
            builder.StartNewVerse(sid, osisId);
        }
    }
}