using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbiblerepository
{
    public class BibleReadingRepository : IBibleReadingRepository
    {
        private readonly IEnumerable<IXmlBibleSearchAction> _searchActions;
        private readonly IXmlPathResolver _pathResolver;

        public BibleReadingRepository(IEnumerable<IXmlBibleSearchAction> searchActions, IXmlPathResolver pathResolver)
        {
            _searchActions = searchActions;
            _pathResolver = pathResolver;
        }

        public BibleReading GetChapter(string bookAbbr, int chapter)
        {
            var search = new BibleSearchRequest
            {
                BookAbbreviation = bookAbbr,
                Chapter = chapter,
                StartVerse = 1
            };
            var builder = new BibleChapterBuilder();
            using (var rdr = XmlReader.Create(Path.Combine(_pathResolver.GetPath(), "kjvfull.xml")))
            {
                rdr.MoveToContent();
                while (rdr.Read())
                {
                    _searchActions.FirstOrDefault(a => a.Matches(rdr, search, builder))?
                        .BuildOn(rdr, builder);
                    if (builder.BuilderClosed) break;
                }
            }
            return builder.GetBibleReading();
        }
    }
}