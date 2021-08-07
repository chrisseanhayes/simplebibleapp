namespace simplebibleapp.xmlbiblerepository
{
    public interface IBibleReadingRepository
    {
        BibleReading GetChapter(string bookAbbr, int chapter);
    }
}