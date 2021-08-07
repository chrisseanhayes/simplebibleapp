namespace simplebibleapp.xmlbiblerepository
{
    public class BookListItem
    {
        public string Testament { get; }
        public string BookName { get; }
        public string SearchAbbr { get; }
        public bool IsChecked { get; }

        public BookListItem(string testament, string bookName, string searchAbbr, bool isChecked)
        {
            Testament = testament;
            BookName = bookName;
            SearchAbbr = searchAbbr;
            IsChecked = isChecked;
        }
    }
}