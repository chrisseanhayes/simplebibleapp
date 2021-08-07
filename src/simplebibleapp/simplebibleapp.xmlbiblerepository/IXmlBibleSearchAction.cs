using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public interface IXmlBibleSearchAction
    {
        bool Matches(XmlReader reader, BibleSearchRequest request, BibleChapterBuilder builder);
        void BuildOn(XmlReader reader, BibleChapterBuilder builder);
    }
}