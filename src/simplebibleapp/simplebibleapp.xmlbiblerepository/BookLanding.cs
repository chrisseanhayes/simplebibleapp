namespace simplebibleapp.xmlbiblerepository
{
    public class BookLanding
    {
        public string BookName { get; }
        public SelectableVerse[] SelectableVerses { get; }

        public BookLanding(string bookName, SelectableVerse[] selectableVerses)
        {
            BookName = bookName;
            SelectableVerses = selectableVerses;
        }
    }
}