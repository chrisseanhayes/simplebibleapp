using System.Collections.Generic;

namespace simplebibleapp.xmlbiblerepository
{
    public interface IBookNameRepository
    {
        IEnumerable<BookListItem> GetBooks();
        IEnumerable<SelectableVerse> GetSelectableVerses(string bookAbbr);
    }
}