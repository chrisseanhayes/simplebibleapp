using System.Collections.Generic;
using System.IO;
using System.Linq;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbiblerepository
{
    public class BookNameRepository : IBookNameRepository
    {
        private readonly IXmlPathResolver _pathResolver;

        public BookNameRepository(IXmlPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }
        public IEnumerable<BookListItem> GetBooks()
        {
            //Server.MapPath("~/Data/Bible/OTBookNames.txt")
            //Server.MapPath("~/Data/Bible/NTBookNames.txt")
            var booknames = File.ReadAllLines(Path.Combine(_pathResolver.GetPath(), "OTBookNames.txt"))
                .Select(n => n + " - Old")
                .Union(File.ReadAllLines(Path.Combine(_pathResolver.GetPath(), "NTBookNames.txt"))
                    .Select(n => n + " - New"))
                .Select(n => new BookListItem
                (
                    testament: n.Split('-').Skip(2).First().Trim(),
                    bookName: n.Split('-').First().Trim(),
                    searchAbbr: n.Split('-').Skip(1).First().Trim(),
                    isChecked: false
                ));
            return booknames;
        }

        public IEnumerable<SelectableVerse> GetSelectableVerses(string bookAbbr)
        {
            //Server.MapPath("~/Data/Bible/kjvfull.xml")
            return from el in ScriptureExtensions.StreamVerses(Path.Combine(_pathResolver.GetPath(), "kjvfull.xml"), bookAbbr)
                select new SelectableVerse
                {
                    Chapter = int.Parse(el.Attribute("sID").Value.Split('.').Skip(1).First()),
                    Verse = int.Parse(el.Attribute("sID").Value.Split('.').Skip(2).First()),
                    BookAbbreviation = el.Attribute("sID").Value.Split('.').First()
                };
        }

    }
}