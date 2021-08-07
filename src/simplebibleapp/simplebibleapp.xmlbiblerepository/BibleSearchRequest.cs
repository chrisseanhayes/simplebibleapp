namespace simplebibleapp.xmlbiblerepository
{
    public class BibleSearchRequest
    {
        public string BookAbbreviation { get; set; }
        public int Chapter { get; set; }
        public int StartVerse { get; set; }
    }
}