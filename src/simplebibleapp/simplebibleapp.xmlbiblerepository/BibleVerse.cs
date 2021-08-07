namespace simplebibleapp.xmlbiblerepository
{
    public class BibleVerse
    {
        public string BookAbbreviation { get; }
        public int Chapter { get; }
        public int Verse { get; }
        public int VerseNumber { get; }
        public IVersePart[] Parts { get; }

        public BibleVerse(string bookAbbreviation, int chapter, int verse, IVersePart[] parts)
        {
            BookAbbreviation = bookAbbreviation;
            Chapter = chapter;
            Verse = verse;
            Parts = parts;
        }
    }
}