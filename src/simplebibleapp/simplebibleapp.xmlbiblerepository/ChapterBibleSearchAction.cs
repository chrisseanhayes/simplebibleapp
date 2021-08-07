using System.Linq;
using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class ChapterBibleSearchAction : IXmlBibleSearchAction
    {
        public bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.Name == "chapter" &&
                   (reader.GetAttribute("osisID")?.StartsWith(request.BookAbbreviation) ?? false) &&
                   int.Parse(reader.GetAttribute("osisID")?.Split('.').Skip(1).First() ?? "-1") == request.Chapter;
        }

        public void BuildOn(XmlReader reader, BibleChapterBuilder builder)
        {
            builder.OpenChapter();
            if (reader.MoveToAttribute("osisID"))
            {
                builder.AddOsisId(reader.Value);
                reader.MoveToElement();
            }
            if (reader.MoveToAttribute("chapterTitle"))
            {
                builder.AddTitle(reader.Value);
                reader.MoveToElement();
            }
        }
    }
}